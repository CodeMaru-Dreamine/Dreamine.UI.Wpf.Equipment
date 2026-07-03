using SharpHook;
using SharpHook.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public class DreamineVirtualKeyboard : UserControl, IDisposable
{
	#region Event Handler

	public EventHandler<LanguageCode>? OnKeyboardLanguageChanged { get; set; }

	#endregion

	#region Variable

	private readonly EventSimulator _eventSimulator = new EventSimulator();
	private readonly HangulComposer _hangulComposer = new();
	private readonly DispatcherTimer _keyboardStateSyncTimer;
	private TaskPoolGlobalHook _hook = null!;

	/// <summary>RunAsync가 이미 호출되어 실행 중인지 여부</summary>
	private volatile bool _hookRunning;

	/// <summary>이벤트 등록 여부(중복 구독 방지)</summary>
	private volatile bool _hookSubscribed;

	protected Panel? _layoutRoot;
	protected IEnumerable<Key>? _keys;
	protected Button? _langBtn;
	protected Button? _inputModeBtn;

	private CancellationTokenSource? _repeatBackspaceCts;
	private Key? _repeatBackspaceKey;

	private DateTime _lastSelfImeToggleAt = DateTime.MinValue;
	private DateTime _lastSelfLangSwitchAt = DateTime.MinValue;
	private DateTime _lastSelfShiftAt = DateTime.MinValue;
	private bool? _pendingKoreanInputMode;
	private DateTime _pendingKoreanInputModeUntil = DateTime.MinValue;
	private static readonly TimeSpan _guard = TimeSpan.FromMilliseconds(350);
	private bool InImeGuard => (DateTime.UtcNow - _lastSelfImeToggleAt) < _guard;
	private bool InLangGuard => (DateTime.UtcNow - _lastSelfLangSwitchAt) < _guard;
	private bool HasPendingKoreanInputMode => _pendingKoreanInputMode.HasValue && DateTime.UtcNow < _pendingKoreanInputModeUntil;
	// 문자 입력 시 대문자/기호를 위해 스스로 Shift를 눌렀다 떼는 동안, 전역 훅이
	// 그걸 실제 Shift 토글로 오인해 상태를 뒤집지 않도록 하는 가드.
	private bool InShiftGuard => (DateTime.UtcNow - _lastSelfShiftAt) < _guard;

	private bool _disposed;

	#endregion

	#region Routed Event

	public static readonly RoutedEvent VirtualKeyDownEvent = EventManager.
		RegisterRoutedEvent(nameof(VirtualKeyDown), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DreamineVirtualKeyboard));

	public event RoutedEventHandler VirtualKeyDown
	{
		add { AddHandler(VirtualKeyDownEvent, value); }
		remove { RemoveHandler(VirtualKeyDownEvent, value); }
	}

	#endregion

	#region Dependency Property

	#region Layout

	public static readonly DependencyProperty LayoutProperty =
		DependencyProperty.Register("Layout", typeof(VkLayout), typeof(DreamineVirtualKeyboard), new PropertyMetadata(VkLayout.Text, OnLayoutChanged));

	public VkLayout Layout
	{
		get { return (VkLayout)GetValue(LayoutProperty); }
		set { SetValue(LayoutProperty, value); }
	}

	private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is DreamineVirtualKeyboard vk)
		{
			var layout = (VkLayout)e.NewValue;
			vk._keys = null;
			vk._langBtn = null;
			vk._inputModeBtn = null;

			vk.IsPasswordLayout = layout == VkLayout.Password;

			vk.Dispatcher.BeginInvoke(() =>
			{
				vk.RefreshKeyboardLayout();
				vk.EnsurePreviewFocus(); 
				vk.TryTogglePreviewVisuals(); 
			}, DispatcherPriority.Loaded);
		}
	}

	#endregion

	#region IsPasswordLayout (Readonly DP)

	private static readonly DependencyPropertyKey IsPasswordLayoutPropertyKey =
		DependencyProperty.RegisterReadOnly(nameof(IsPasswordLayout), typeof(bool), typeof(DreamineVirtualKeyboard), new PropertyMetadata(false, OnIsPasswordLayoutChanged));

	public static readonly DependencyProperty IsPasswordLayoutProperty = IsPasswordLayoutPropertyKey.DependencyProperty;

	public bool IsPasswordLayout
	{
		get => (bool)GetValue(IsPasswordLayoutProperty);
		private set => SetValue(IsPasswordLayoutPropertyKey, value);
	}

	private static void OnIsPasswordLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is DreamineVirtualKeyboard vk)
		{
			vk.TryTogglePreviewVisuals();
		}
	}

	#endregion

	#endregion

	#region Property

	public LanguageCode CurrentLang { get; private set; }
	public bool IsPressedShift { get; private set; }

	/// <summary>
	/// CapsLock은 로컬 토글이 아니라 OS 실제 상태를 단일 소스로 읽는다.
	/// → 물리 키보드로 CapsLock을 눌러도 그대로 반영되고, 가상/물리가 서로 안 싸운다.
	/// </summary>
	public bool IsPressedCapsLock => Keyboard.IsKeyToggled(System.Windows.Input.Key.CapsLock);

	public bool IsPressedCtrl { get; set; }
	public bool ImeMode { get; private set; }

	#endregion

	#region Constructor

	public DreamineVirtualKeyboard()
	{
		_keyboardStateSyncTimer = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = TimeSpan.FromMilliseconds(160),
		};
		_keyboardStateSyncTimer.Tick += SynchronizeKeyboardState;

		Loaded += KeyboardUserControl_Loaded;
		Unloaded += KeyboardUserControl_Unloaded;
		IsVisibleChanged += KeyboardUserControl_IsVisibleChanged;
		AddHandler(PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)BackspacePointerReleased, true);
		AddHandler(PreviewTouchUpEvent, (EventHandler<TouchEventArgs>)BackspacePointerReleased, true);
		AddHandler(LostMouseCaptureEvent, (MouseEventHandler)BackspaceMouseCaptureLost, true);
		InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;

		_ = RegisterHookEventsAsync();
	}

	#endregion

	#region Private Method

	private void FindElements()
	{
		if (_layoutRoot is null && FindName("KeyboardLayoutRoot") is Panel layoutRoot)
		{
			_layoutRoot = layoutRoot;
		}

		if (_layoutRoot != null && _langBtn is null && _layoutRoot.FindFirstVisualChild<Button>("_langBtn") is { } langBtn)
		{
			_langBtn = langBtn;
		}

		if (_layoutRoot != null && _inputModeBtn is null && _layoutRoot.FindFirstVisualChild<Button>("_inputModeBtn") is { } inputModeBtn)
		{
			_inputModeBtn = inputModeBtn;
		}

		if (_layoutRoot != null && (_keys is null || !_keys.Any()))
		{
			_keys = _layoutRoot.FindVisualChildren<Key>();
		}
	}

	private async Task RegisterHookEventsAsync()
	{
		await Task.Yield();
		if (_hookRunning)
			return;

		_hook ??= new TaskPoolGlobalHook();

		if (!_hookSubscribed)
		{
			_hook.KeyPressed += Hook_KeyPressed;
			_hook.KeyReleased += Hook_KeyReleased;
			_hook.MouseReleased += Hook_MouseReleased;
			_hookSubscribed = true;
		}

		_hookRunning = true;
		_ = Task.Run(async () =>
		{
			try
			{
				await _hook.RunAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[VK Hook] RunAsync failed: {ex.Message}");
			}
			finally
			{
				_hookRunning = false; 
			}
		});
	}

	private void UnregisterHookEvents()
	{
		if (_hook is null)
			return;

		// 실행 중이면 중지
		if (_hookRunning)
		{
			_hook.Dispose();
			_hookRunning = false;
		}

		// 이벤트 구독 해제 (필요 시 완전 해제)
		if (_hookSubscribed)
		{
			_hook.KeyPressed -= Hook_KeyPressed;
			_hook.KeyReleased -= Hook_KeyReleased;
			_hook.MouseReleased -= Hook_MouseReleased;
			_hookSubscribed = false;
		}

		//_hook = null;  // 재사용 의도가 없으면 해제
	}	

	protected void UpdateKeys()
	{
		UpdateLangKey();

		if (!InImeGuard)
			ImeMode = ImeHelper.GetImeMode();

		if (_keys != null && _keys.Any())
		{
			foreach (var keyButton in _keys)
			{
				var useShift = Layout == VkLayout.Text || Layout == VkLayout.Password;
				keyButton.UpdateKey(useShift ? IsPressedShift : false, IsPressedCapsLock, CurrentLang, ImeMode);
			}
		}
	}

	private void EnsurePreviewFocus()
	{
		if (_layoutRoot == null)
			_layoutRoot = FindName("KeyboardLayoutRoot") as Panel;

		if (Layout == VkLayout.Password)
		{
			if (_layoutRoot?.FindFirstVisualChild<PasswordBox>("VkbPasswordBox") is { } pb && !pb.IsKeyboardFocusWithin)
			{
				pb.Focus();
			}
		}
		else
		{
			if (_layoutRoot?.FindFirstVisualChild<TextBox>("VkbTextBox") is { } tb && !tb.IsKeyboardFocusWithin)
			{
				tb.Focus();
				tb.CaretIndex = tb.Text?.Length ?? 0;
			}
		}
	}

	protected void UpdateLangKey()
	{
		if (_langBtn != null)
		{
			_langBtn.Visibility = Visibility.Collapsed;
		}
	}

	protected bool UpdateInputModeKey(bool readSystemIme = true)
	{
		var imeModeChanged = false;

		if (readSystemIme && !InImeGuard)
		{
			var imeMode = ImeHelper.GetImeMode();
			if (ImeMode != imeMode)
			{
				ImeMode = imeMode;
				imeModeChanged = true;
			}
		}

		if (_inputModeBtn != null)
		{
			_inputModeBtn.Content = CurrentLang == LanguageCode.ko_KR && ImeMode ? "가" : "abc";
		}

		return imeModeChanged;
	}

	protected void RefreshKeyboardLayout(string lang = "")
	{
		SetLanguage(lang);
		FindElements();
		UpdateInputModeKey();
		UpdateKeys();
	}

	private void SynchronizeKeyboardState(object? sender, EventArgs e)
	{
		if (!IsVisible || DesignerProperties.GetIsInDesignMode(this))
			return;

		if (HasPendingKoreanInputMode)
		{
			RefreshKeyboardVisualState(false);
			return;
		}

		_pendingKoreanInputMode = null;

		var previousLang = CurrentLang;
		var previousImeMode = ImeMode;

		if (!InLangGuard)
			SetLanguage(InputLanguageManager.Current.CurrentInputLanguage.Name);

		UpdateInputModeKey();

		if (previousLang != CurrentLang || previousImeMode != ImeMode)
			UpdateKeys();
	}

	private bool TryHandleComposedTextKey(Key key)
	{
		if (Layout != VkLayout.Text && Layout != VkLayout.Password)
			return false;

		if (key.KeyCode < KeyCode.VcA || key.KeyCode > KeyCode.VcZ)
			return false;

		var text = key.GetDisplayText(IsPressedShift, IsPressedCapsLock, CurrentLang, ImeMode);
		if (!HangulComposer.IsComposableJamo(text))
			return false;

		var edit = _hangulComposer.Input(text, GetTextBeforeCaret());
		ReplaceTextTail(edit.ReplaceCount, edit.Text);
		return true;
	}

	private void InsertRawText(string text)
	{
		_hangulComposer.Reset();
		ReplaceTextTail(0, text);
	}

	private void ReplaceTextTail(int replaceCount, string text)
	{
		if (Layout == VkLayout.Password)
		{
			var password = VkbPasswordBoxFromTree()?.Password ?? string.Empty;
			var keep = Math.Max(0, password.Length - replaceCount);
			SetPassword(password[..keep] + text);
			return;
		}

		if (VkbTextBoxFromTree() is not { } textBox)
			return;

		var currentText = textBox.Text ?? string.Empty;
		var selectionStart = Math.Clamp(textBox.SelectionStart, 0, currentText.Length);
		var selectionLength = Math.Clamp(textBox.SelectionLength, 0, currentText.Length - selectionStart);

		if (selectionLength > 0)
		{
			textBox.SelectedText = text;
			textBox.CaretIndex = selectionStart + text.Length;
		}
		else
		{
			var removeStart = Math.Max(0, selectionStart - replaceCount);
			removeStart = Math.Min(removeStart, currentText.Length);
			var removeLength = Math.Clamp(selectionStart - removeStart, 0, currentText.Length - removeStart);
			textBox.Text = currentText.Remove(removeStart, removeLength).Insert(removeStart, text);
			textBox.CaretIndex = removeStart + text.Length;
		}

		textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
	}

	private string GetTextBeforeCaret()
	{
		if (Layout == VkLayout.Password)
			return VkbPasswordBoxFromTree()?.Password ?? string.Empty;

		if (VkbTextBoxFromTree() is not { } textBox)
			return string.Empty;

		var caret = Math.Clamp(textBox.CaretIndex, 0, textBox.Text.Length);
		return textBox.Text[..caret];
	}

	private void SetPassword(string password)
	{
		if (VkbPasswordBoxFromTree() is { } passwordBox)
			passwordBox.Password = password;
	}

	private TextBox? VkbTextBoxFromTree()
	{
		var window = Window.GetWindow(this) ?? this as DependencyObject;
		return window == null ? null : FindByNameInVisualTree<TextBox>(window, "VkbTextBox");
	}

	private PasswordBox? VkbPasswordBoxFromTree()
	{
		var window = Window.GetWindow(this) ?? this as DependencyObject;
		return window == null ? null : FindByNameInVisualTree<PasswordBox>(window, "VkbPasswordBox");
	}

	/// <summary>
	/// 문자 키를 입력한다. Shift 토글/CapsLock 상태에 따라 물리 Shift를 한 글자만
	/// 감싸서 대문자/기호를 만든다. 상태는 클릭 핸들러가 직접 관리하므로 전역 훅이
	/// 꺼져 있어도(디버거 실행 등) 동작한다.
	/// </summary>
	private void SimulateCharKey(Key key)
	{
		var kc = key.KeyCode;

		// 대문자/기호는 '가상 Shift 토글'로만 결정한다.
		// CapsLock(영문 대문자화)은 OS가 주입된 문자 키에 자동 적용하므로 여기서 감싸지 않는다.
		var useShift = IsPressedShift;

		if (useShift)
		{
			_lastSelfShiftAt = DateTime.UtcNow;
			_eventSimulator.SimulateKeyPress(KeyCode.VcLeftShift);
			_eventSimulator.SimulateKeyPress(kc);
			_eventSimulator.SimulateKeyRelease(kc);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
			_lastSelfShiftAt = DateTime.UtcNow;
		}
		else
		{
			_eventSimulator.SimulateKeyPress(kc);
			_eventSimulator.SimulateKeyRelease(kc);
		}
	}

	private void ReleaseBackspaceRepeat()
	{
		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.BeginInvoke(new Action(ReleaseBackspaceRepeat), DispatcherPriority.Send);
			return;
		}

		if (_repeatBackspaceCts != null)
		{
			_repeatBackspaceCts.Cancel();
			_repeatBackspaceCts.Dispose();
			_repeatBackspaceCts = null;
		}

		if (_repeatBackspaceKey?.IsMouseCaptured == true)
			_repeatBackspaceKey.ReleaseMouseCapture();

		_repeatBackspaceKey = null;
		_eventSimulator.SimulateKeyRelease(KeyCode.VcBackspace);
	}

	private void StartBackspaceRepeat(Key key)
	{
		if (_repeatBackspaceCts != null)
			return;

		_repeatBackspaceKey = key;
		key.CaptureMouse();
		SimulateBackspaceStroke();

		_repeatBackspaceCts = new CancellationTokenSource();
		var token = _repeatBackspaceCts.Token;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(400, token).ConfigureAwait(false);
				while (!token.IsCancellationRequested)
				{
					SimulateBackspaceStroke();
					await Task.Delay(50, token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}, token);
	}

	private void SimulateBackspaceStroke()
	{
		_eventSimulator.SimulateKeyPress(KeyCode.VcBackspace);
		_eventSimulator.SimulateKeyRelease(KeyCode.VcBackspace);
	}

	private string NormalizeLanguageString(string lang)
	{
		if (lang == null)
			return "";

		char[] hiddenChars =
		{
		'\u200B', // zero width space
		'\u200C',
		'\u200D',
		'\u2060',
		'\uFEFF', // BOM
		'\u00A0'  // NBSP
	};

		return new string(lang.Where(c => !hiddenChars.Contains(c)).ToArray()).Trim();
	}

	private void SetLanguage(string lang)
	{
		if (string.IsNullOrEmpty(lang))
		{
			lang = InputLanguageManager.Current.CurrentInputLanguage.Name;
		}

		lang = NormalizeLanguageString(lang);

		var currentLang = lang switch
		{
			"ko-KR" => LanguageCode.ko_KR,
			"vi-VN" => LanguageCode.vi_VN,
			"zh-CN" => LanguageCode.zh_CN,
			_ => LanguageCode.en_US
		};

		if (CurrentLang == currentLang)
			return;

		CurrentLang = currentLang;
		OnKeyboardLanguageChanged?.Invoke(this, CurrentLang);
	}

	private bool SetKoreanEnglishMode(bool useKorean)
	{
		_lastSelfLangSwitchAt = DateTime.UtcNow;
		_lastSelfImeToggleAt = DateTime.UtcNow;
		_pendingKoreanInputMode = useKorean;
		_pendingKoreanInputModeUntil = DateTime.UtcNow + TimeSpan.FromMilliseconds(900);

		var targetName = useKorean ? "ko-KR" : "en-US";
		try
		{
			var available = InputLanguageManager.Current.AvailableInputLanguages;
			var match = available?
				.Cast<System.Globalization.CultureInfo>()
				.FirstOrDefault(c => string.Equals(c.Name, targetName, StringComparison.OrdinalIgnoreCase));

			if (match != null)
			{
				InputLanguageManager.Current.CurrentInputLanguage = match;
				SetLanguage(match.Name);
			}
			else
			{
				SetLanguage(targetName);
			}

			EnsurePreviewFocus();
			ApplyDesiredImeMode(useKorean);
			RefreshKeyboardVisualState(false);
			RecheckKeyboardStateSoon(useKorean);
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[VK] SetKoreanEnglishMode failed: {ex.Message}");
		}

		return false;
	}

	private void ApplyDesiredImeMode(bool useKorean)
	{
		var currentImeMode = ImeHelper.GetImeMode();
		if (currentImeMode != useKorean)
		{
			if (!ImeHelper.SetImeMode(useKorean))
			{
				switch (CurrentLang)
				{
					case LanguageCode.ko_KR:
						_eventSimulator.SimulateKeyPress(KeyCode.VcHangul);
						_eventSimulator.SimulateKeyRelease(KeyCode.VcHangul);
						break;
					case LanguageCode.zh_CN:
						_eventSimulator.SimulateKeyPress(KeyCode.VcLeftShift);
						_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
						break;
				}
			}
		}

		ImeMode = useKorean;
	}

	private bool IsKoreanInputActive()
	{
		if (HasPendingKoreanInputMode)
			return _pendingKoreanInputMode == true;

		var currentLanguage = InputLanguageManager.Current.CurrentInputLanguage.Name;
		var isKoreanLanguage = string.Equals(
			NormalizeLanguageString(currentLanguage),
			"ko-KR",
			StringComparison.OrdinalIgnoreCase);

		return isKoreanLanguage && ImeHelper.GetImeMode();
	}

	private void RecheckKeyboardStateSoon(bool useKorean)
	{
		var remaining = 5;
		DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
		timer.Tick += (_, __) =>
		{
			EnsurePreviewFocus();
			ApplyDesiredImeMode(useKorean);
			RefreshKeyboardVisualState(false);

			if (--remaining <= 0)
			{
				timer.Stop();
				_pendingKoreanInputMode = null;
				RefreshKeyboardVisualState();
			}
		};
		timer.Start();
	}

	private void RefreshKeyboardVisualState(bool readSystemIme = true)
	{
		if (HasPendingKoreanInputMode)
		{
			ImeMode = _pendingKoreanInputMode == true;
			readSystemIme = false;
		}

		UpdateInputModeKey(readSystemIme);
		UpdateKeys();
		EnsurePreviewFocus();
	}

	private bool IsNumLockOn()
	{
		return Convert.ToBoolean(Win32Api.GetKeyState((int)KeyCode.VcNumLock) & 0x0001);
	}

	private void TryTogglePreviewVisuals()
	{
		var window = Window.GetWindow(this) ?? this as DependencyObject;
		if (window == null) return;

		var vkbText = FindByNameInVisualTree<TextBox>(window, "VkbTextBox");
		var vkbPwd = FindByNameInVisualTree<PasswordBox>(window, "VkbPasswordBox");

		if (vkbText != null)
			vkbText.Visibility = IsPasswordLayout ? Visibility.Collapsed : Visibility.Visible;

		if (vkbPwd != null)
			vkbPwd.Visibility = IsPasswordLayout ? Visibility.Visible : Visibility.Collapsed;
	}

	private static T? FindByNameInVisualTree<T>(DependencyObject root, string name) where T : FrameworkElement
	{
		if (root is T fe && fe.Name == name)
			return fe;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			var found = FindByNameInVisualTree<T>(child, name);
			if (found != null) return found;
		}
		return null;
	}

#endregion

	#region Event Handler

	private void OnInputLanguageChanged(object? sender, InputLanguageEventArgs e)
	{
		_hangulComposer.Reset();

		if (InLangGuard)
		{
			SetLanguage(e.NewLanguage.Name);
			UpdateInputModeKey();
			UpdateKeys();
			EnsurePreviewFocus();
			return;
		}

		RefreshKeyboardLayout(e.NewLanguage.Name);
		EnsurePreviewFocus();
	}

	private void KeyboardUserControl_Loaded(object? sender, RoutedEventArgs e)
	{
		if (DesignerProperties.GetIsInDesignMode(this))
			return;

		RemoveHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);
		AddHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);

		Dispatcher.BeginInvoke(() =>
		{
			RefreshKeyboardLayout();
			TryTogglePreviewVisuals();
			EnsurePreviewFocus();
			_keyboardStateSyncTimer.Start();
		}, DispatcherPriority.SystemIdle);
	}

	/// <summary>
	/// @brief Visual 트리에서 떨어질 때(창 닫힘 포함) 안전 정리.
	/// </summary>
	private void KeyboardUserControl_Unloaded(object? sender, RoutedEventArgs e)
	{
		_keyboardStateSyncTimer.Stop();
		Dispose(); // 안전
	}

	private void KeyboardUserControl_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is not bool isVisible) return;

		if (!isVisible)
		{
			_keyboardStateSyncTimer.Stop();

			if (IsPressedShift)
			{
				IsPressedShift = false;
				_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
			}
			// CapsLock은 OS 상태를 그대로 두고 건드리지 않는다(단일 소스).
		}
		else if (IsLoaded)
		{
			SynchronizeKeyboardState(this, EventArgs.Empty);
			_keyboardStateSyncTimer.Start();
		}
	}

	private void Hook_KeyPressed(object? sender, KeyboardHookEventArgs e)
	{
		var keyCode = e.Data.KeyCode;
		Dispatcher.BeginInvoke(() =>
		{
			var refreshLayout = false;
			if (UpdateInputModeKey())
				refreshLayout = true;

			switch (keyCode)
			{
				case KeyCode.VcLeftShift:
				case KeyCode.VcRightShift:
					// 문자 입력용 자체 Shift 시뮬레이션은 무시(토글 상태 보존).
					if (!InShiftGuard)
					{
						if (Layout == VkLayout.Text || Layout == VkLayout.Password)
							IsPressedShift = true;
						if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
							shiftKey.IsPressed = true;
						refreshLayout = true;
					}
					break;
				case KeyCode.VcCapsLock:
					// OS CapsLock 토글 결과를 표시에 반영(상태 자체는 OS가 소유).
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcCapsLock) is { } capsLockKey)
						capsLockKey.IsPressed = IsPressedCapsLock;
					refreshLayout = true;
					break;
				case KeyCode.VcLeftControl:
				case KeyCode.VcRightControl:
					if (Layout == VkLayout.Text || Layout == VkLayout.Password)
						IsPressedCtrl = true;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftControl) is { } ctrlKey)
						ctrlKey.IsPressed = true;
					break;
				default:
					if (keyCode >= KeyCode.VcA && keyCode <= KeyCode.VcZ)
						_hangulComposer.Reset();

					if (_keys?.FirstOrDefault(k => k.KeyCode == keyCode) is { } keyBtn)
						keyBtn.IsPressed = true;
					break;
			}

			if (refreshLayout)
				UpdateKeys();
		}, DispatcherPriority.ApplicationIdle);
	}

	private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e)
	{
		var keyCode = e.Data.KeyCode;
		Dispatcher.BeginInvoke(() =>
		{
			switch (keyCode)
			{
				case KeyCode.VcLeftShift:
				case KeyCode.VcRightShift:
					if (!InShiftGuard)
					{
						IsPressedShift = false;
						if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
							shiftKey.IsPressed = false;
						UpdateKeys();
					}
					break;
				case KeyCode.VcCapsLock:
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcCapsLock) is { } capsLockKey)
						capsLockKey.IsPressed = IsPressedCapsLock;
					UpdateKeys();
					break;
				case KeyCode.VcLeftControl:
				case KeyCode.VcRightControl:
					IsPressedCtrl = false;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftControl) is { } ctrlKey)
						ctrlKey.IsPressed = false;
					break;
				default:
					if (_keys?.FirstOrDefault(k => k.KeyCode == keyCode) is { } keyBtn)
						keyBtn.IsPressed = false;
					break;
			}
		}, DispatcherPriority.ApplicationIdle);
	}

	private void Hook_MouseReleased(object? sender, MouseHookEventArgs e)
	{
		ReleaseBackspaceRepeat();
	}

	private void BackspacePointerReleased(object? sender, InputEventArgs e)
	{
		ReleaseBackspaceRepeat();
	}

	private void BackspaceMouseCaptureLost(object? sender, MouseEventArgs e)
	{
		if (_repeatBackspaceCts != null && _repeatBackspaceKey?.IsMouseCaptured != true)
			ReleaseBackspaceRepeat();
	}

	private void KeyClick(object? sender, RoutedEventArgs e)
	{
		if (Layout != VkLayout.Text && Layout != VkLayout.Password)
		{
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcRightShift);

			if (!IsNumLockOn())
			{
				_eventSimulator.SimulateKeyPress(KeyCode.VcNumLock);
				_eventSimulator.SimulateKeyRelease(KeyCode.VcNumLock);
			}
		}

		if (e.OriginalSource is Button { Name: "_langBtn" })
		{
			_hangulComposer.Reset();
			SetKoreanEnglishMode(!IsKoreanInputActive());
			return;
		}

		if (e.OriginalSource is Button { Name: "_inputModeBtn" })
		{
			_hangulComposer.Reset();
			SetKoreanEnglishMode(!IsKoreanInputActive());
			return;
		}

		if (e.OriginalSource is not Key key)
			return;

		var keyCode = key.KeyCode;

		switch (keyCode)
		{
			// Shift는 토글(lock). 물리 Shift를 눌러두지 않고 상태만 들고 있다가,
			// 문자 입력 때 SimulateCharKey에서 한 글자씩 감싼다. → 전역 훅 없이도 동작.
			case KeyCode.VcLeftShift:
			case KeyCode.VcRightShift:
				IsPressedShift = !IsPressedShift;
				key.IsPressed = IsPressedShift;
				UpdateKeys();
				break;

			// CapsLock: 실제 OS CapsLock을 토글하고, 반영은 OS 상태를 다시 읽어서 한다.
			case KeyCode.VcCapsLock:
				_eventSimulator.SimulateKeyPress(keyCode);
				_eventSimulator.SimulateKeyRelease(keyCode);
				DispatcherTimer tCaps = new() { Interval = TimeSpan.FromMilliseconds(120) };
				tCaps.Tick += (_, __) =>
				{
					tCaps.Stop();
					key.IsPressed = IsPressedCapsLock;
					UpdateKeys();
				};
				tCaps.Start();
				break;

			case KeyCode.VcLeftControl:
				IsPressedCtrl = !IsPressedCtrl;
				key.IsPressed = IsPressedCtrl;
				if (IsPressedCtrl) _eventSimulator.SimulateKeyPress(keyCode);
				else _eventSimulator.SimulateKeyRelease(keyCode);
				break;

			case KeyCode.VcBackspace:
				_hangulComposer.Reset();
				StartBackspaceRepeat(key);
				break;

			case KeyCode.VcEnter:
				_hangulComposer.Reset();
				_eventSimulator.SimulateKeyPress(keyCode);
				_eventSimulator.SimulateKeyRelease(keyCode);
				break;

			case KeyCode.VcTab:
				_hangulComposer.Reset();
				_eventSimulator.SimulateTextEntry("    ");
				break;

			case KeyCode.VcSpace:
				InsertRawText(" ");
				break;

			default:
				if (IsPressedCtrl)
				{
					_hangulComposer.Reset();
					// Ctrl 조합(단축키)은 한 번만 적용하고 해제(one-shot).
					_eventSimulator.SimulateKeyPress(keyCode);
					_eventSimulator.SimulateKeyRelease(keyCode);
					_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);
					IsPressedCtrl = false;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftControl) is { } ctrlKey)
						ctrlKey.IsPressed = false;
				}
				else
				{
					if (!TryHandleComposedTextKey(key))
					{
						_hangulComposer.Reset();
						SimulateCharKey(key);
					}
				}
				break;
		}

		RaiseEvent(new RoutedEventArgs(VirtualKeyDownEvent, key.KeyCode));
	}

	#endregion

	#region Disposable

	public void Dispose() => Dispose(true);

	/// <summary>
	/// @brief Dispose 본체(표준 패턴).
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed) return;
		_disposed = true;

		try
		{
			// 공통: 눌린 키/반복/훅 정리
			_keyboardStateSyncTimer.Stop();
			ReleasePressedKeys();
			ReleaseBackspaceRepeat();
			UnregisterHookEvents();      // _hook.Dispose() 포함

			if (disposing)
			{
				_keyboardStateSyncTimer.Tick -= SynchronizeKeyboardState;

				// 관리 리소스/이벤트 해제
				Loaded -= KeyboardUserControl_Loaded;
				Unloaded -= KeyboardUserControl_Unloaded;
				IsVisibleChanged -= KeyboardUserControl_IsVisibleChanged;
				InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;

				// RoutedEvent 핸들러 해제
				RemoveHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);

				// SharpHook 자체 해제
				_hook?.Dispose();
				_hook = null!;

				// 참조 끊기(시각 트리 객체들)
				_layoutRoot = null;
				_keys = null;
				_langBtn = null;
				_inputModeBtn = null;
			}
		}
		catch (Exception ex) { Debug.WriteLine($"[VK] Dispose error: {ex.Message}"); }
		finally
		{
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// @brief 파이널라이저(보호용).
	/// </summary>
	~DreamineVirtualKeyboard() => Dispose(false);


	public void ReleasePressedKeys()
	{
		ReleaseBackspaceRepeat();

		if (IsPressedCapsLock)
			_eventSimulator.SimulateKeyRelease(KeyCode.VcCapsLock);

		if (IsPressedShift)
		{
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcRightShift);
		}

		if (IsPressedCtrl)
		{
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcRightControl);
		}
	}

	#endregion
}

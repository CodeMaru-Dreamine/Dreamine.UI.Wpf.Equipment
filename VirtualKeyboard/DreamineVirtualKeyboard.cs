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
	private TaskPoolGlobalHook _hook = null!;

	/// <summary>RunAsync가 이미 호출되어 실행 중인지 여부</summary>
	private bool _hookRunning;

	/// <summary>이벤트 등록 여부(중복 구독 방지)</summary>
	private bool _hookSubscribed;

	protected Panel? _layoutRoot;
	protected IEnumerable<Key>? _keys;
	protected Button? _langBtn;
	protected Button? _inputModeBtn;

	private CancellationTokenSource? _repeatBackspaceCts;

	private DateTime _lastSelfImeToggleAt = DateTime.MinValue;
	private DateTime _lastSelfLangSwitchAt = DateTime.MinValue;
	private static readonly TimeSpan _guard = TimeSpan.FromMilliseconds(350);
	private bool InImeGuard => (DateTime.UtcNow - _lastSelfImeToggleAt) < _guard;
	private bool InLangGuard => (DateTime.UtcNow - _lastSelfLangSwitchAt) < _guard;

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
	public bool IsPressedCapsLock { get; private set; }
	public bool IsPressedCtrl { get; set; }
	public bool ImeMode { get; private set; }

	#endregion

	#region Constructor

	public DreamineVirtualKeyboard()
	{
		Loaded += KeyboardUserControl_Loaded;
		Unloaded += KeyboardUserControl_Unloaded;
		IsVisibleChanged += KeyboardUserControl_IsVisibleChanged;
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

		bool forceHook = string.Equals(
			Environment.GetEnvironmentVariable("VK_FORCE_HOOK"),
			"1", StringComparison.Ordinal);

		if (Debugger.IsAttached && !forceHook)
		{
			_hookRunning = false;
			return;
		}

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
			catch
			{
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
			_langBtn.Content = CurrentLang.ToString().Split("_")[1].ToUpper();
		}
	}

	protected bool UpdateInputModeKey()
	{
		var imeModeChanged = false;

		if (!InImeGuard)
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
			switch (CurrentLang)
			{
				case LanguageCode.ko_KR:
					_inputModeBtn.Content = ImeMode ? "가" : "abc";
					break;
				case LanguageCode.zh_CN:
					_inputModeBtn.Content = ImeMode ? "中" : "英";
					break;
				default:
					_inputModeBtn.Content = "abc";
					break;
			}
		}

		return imeModeChanged;
	}

	protected void RefreshKeyboardLayout(string lang = "")
	{
		//SetLanguage(lang);
		FindElements();
		UpdateInputModeKey();
		UpdateKeys();
	}

	private void ReleaseBackspaceRepeat()
	{
		if (_repeatBackspaceCts != null)
		{
			_repeatBackspaceCts.Cancel();
			_repeatBackspaceCts.Dispose();
			_repeatBackspaceCts = null;
			_eventSimulator.SimulateKeyRelease(KeyCode.VcBackspace);
		}
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

		CurrentLang = lang switch
		{
			"ko-KR" => LanguageCode.ko_KR,
			"vi-VN" => LanguageCode.vi_VN,
			"zh-CN" => LanguageCode.zh_CN,
			_ => LanguageCode.en_US
		};

		OnKeyboardLanguageChanged?.Invoke(this, CurrentLang);
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

	private void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
	{
		if (InLangGuard)
		{
			SetLanguage(e.NewLanguage.Name);
			UpdateLangKey();
			return;
		}

		RefreshKeyboardLayout(e.NewLanguage.Name);
		EnsurePreviewFocus();
	}

	private void KeyboardUserControl_Loaded(object sender, RoutedEventArgs e)
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
		}, DispatcherPriority.SystemIdle);
	}

	/// <summary>
	/// @brief Visual 트리에서 떨어질 때(창 닫힘 포함) 안전 정리.
	/// </summary>
	private void KeyboardUserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		Dispose(); // 안전
	}

	private void KeyboardUserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is not bool isVisible) return;

		if (!isVisible)
		{
			if (IsPressedShift)
			{
				IsPressedShift = false;
				_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
			}

			if (IsPressedCapsLock)
			{
				IsPressedCapsLock = false;
				_eventSimulator.SimulateKeyRelease(KeyCode.VcCapsLock);
			}
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
					if (Layout == VkLayout.Text || Layout == VkLayout.Password)
						IsPressedShift = true;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
						shiftKey.IsPressed = true;
					refreshLayout = true;
					break;
				case KeyCode.VcCapsLock:
					IsPressedCapsLock = !IsPressedCapsLock;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcCapsLock) is { } capsLockKey)
						capsLockKey.IsPressed = true;
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
					IsPressedShift = false;
					if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
						shiftKey.IsPressed = false;
					UpdateKeys();
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

	private void KeyClick(object sender, RoutedEventArgs e)
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
			_lastSelfLangSwitchAt = DateTime.UtcNow;

			_eventSimulator.SimulateKeyPress(KeyCode.VcLeftAlt);
			_eventSimulator.SimulateKeyPress(KeyCode.VcLeftShift);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftAlt);
			_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);

			DispatcherTimer t = new() { Interval = TimeSpan.FromMilliseconds(120) };
			t.Tick += (_, __) => { t.Stop(); RefreshKeyboardLayout(); };
			t.Start();
			return;
		}

		if (e.OriginalSource is Button { Name: "_inputModeBtn" })
		{
			_lastSelfImeToggleAt = DateTime.UtcNow;

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
				default:
					break;
			}

			DispatcherTimer t = new() { Interval = TimeSpan.FromMilliseconds(120) };
			t.Tick += (_, __) =>
			{
				t.Stop();
				UpdateInputModeKey();
				UpdateKeys();
				EnsurePreviewFocus();
			};
			t.Start();
			return;
		}

		if (e.OriginalSource is not Key key)
			return;

		var keyCode = key.KeyCode;

		switch (keyCode)
		{
			case KeyCode.VcLeftShift:
				IsPressedShift = !IsPressedShift;
				key.IsPressed = IsPressedShift;
				if (IsPressedShift) _eventSimulator.SimulateKeyPress(keyCode);
				else _eventSimulator.SimulateKeyRelease(keyCode);
				break;

			case KeyCode.VcLeftControl:
				IsPressedCtrl = !IsPressedCtrl;
				key.IsPressed = IsPressedCtrl;
				if (IsPressedCtrl) _eventSimulator.SimulateKeyPress(keyCode);
				else _eventSimulator.SimulateKeyRelease(keyCode);
				break;

			case KeyCode.VcBackspace:
				if (_repeatBackspaceCts != null) return;

				_eventSimulator.SimulateKeyPress(keyCode);
				_repeatBackspaceCts = new CancellationTokenSource();
				var token = _repeatBackspaceCts.Token;
				_ = Task.Run(async () =>
				{
					await Task.Delay(400);
					while (!token.IsCancellationRequested)
					{
						_eventSimulator.SimulateKeyPress(keyCode);
						await Task.Delay(50, token);
					}
				}, token);
				break;

			case KeyCode.VcEnter:
				_eventSimulator.SimulateKeyPress(keyCode);
				_eventSimulator.SimulateKeyRelease(keyCode);
				break;

			case KeyCode.VcTab:
				_eventSimulator.SimulateTextEntry("    ");
				break;

			default:
				_eventSimulator.SimulateKeyPress(keyCode);
				_eventSimulator.SimulateKeyRelease(keyCode);

				if (IsPressedCtrl)
					_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);

				if (IsPressedShift)
					_eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
				break;
		}

		if (keyCode is KeyCode.VcLeftShift or KeyCode.VcRightShift or KeyCode.VcCapsLock)
			Dispatcher.Invoke(UpdateKeys);

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
			ReleasePressedKeys();
			ReleaseBackspaceRepeat();
			UnregisterHookEvents();      // _hook.Dispose() 포함

			if (disposing)
			{
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
		catch { /* swallow on shutdown */ }
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

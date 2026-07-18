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

/// <summary>
/// \if KO
/// <para>전역 물리 입력과 동기화하며 다국어·한글 조합·반복 키 입력을 제공하는 가상 키보드 컨트롤입니다.</para>
/// \endif
/// \if EN
/// <para>Represents a virtual-keyboard control synchronized with global physical input and supporting multilingual text, Hangul composition, and key repeat.</para>
/// \endif
/// </summary>
public class DreamineVirtualKeyboard : UserControl, IDisposable
{
	#region Event Handler

	/// <summary>
	/// \if KO
	/// <para>키보드 언어가 변경될 때 호출할 처리기를 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets the handler invoked when the keyboard language changes.</para>
	/// \endif
	/// </summary>
	public EventHandler<LanguageCode>? OnKeyboardLanguageChanged { get; set; }

	#endregion

	#region Variable

	/// <summary>
	/// \if KO
	/// <para>네이티브 키와 텍스트 입력을 주입하는 SharpHook 시뮬레이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the SharpHook simulator used to inject native keys and text.</para>
	/// \endif
	/// </summary>
	private readonly EventSimulator _eventSimulator = new EventSimulator();
	/// <summary>
	/// \if KO
	/// <para>직접 입력한 한글 자모의 조합 상태를 관리합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the composer for directly entered Hangul jamo.</para>
	/// \endif
	/// </summary>
	private readonly HangulComposer _hangulComposer = new();
	/// <summary>
	/// \if KO
	/// <para>표시 중 시스템 언어·IME 상태를 주기적으로 동기화하는 타이머입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the timer that periodically synchronizes system language and IME state while visible.</para>
	/// \endif
	/// </summary>
	private readonly DispatcherTimer _keyboardStateSyncTimer;
	/// <summary>
	/// \if KO
	/// <para>물리 키보드와 마우스 상태를 감지하는 전역 후크입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the global hook that observes physical keyboard and mouse state.</para>
	/// \endif
	/// </summary>
	private TaskPoolGlobalHook _hook = null!;

	/// <summary>
	/// \if KO
	/// <para>전역 후크 실행 루프가 이미 실행 중인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether the global-hook run loop is already active.</para>
	/// \endif
	/// </summary>
	private volatile bool _hookRunning;

	/// <summary>
	/// \if KO
	/// <para>전역 후크 이벤트가 구독되었는지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether global-hook events have been subscribed.</para>
	/// \endif
	/// </summary>
	private volatile bool _hookSubscribed;

	/// <summary>
	/// \if KO
	/// <para>현재 템플릿의 키보드 레이아웃 루트를 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the keyboard-layout root from the current template.</para>
	/// \endif
	/// </summary>
	protected Panel? _layoutRoot;
	/// <summary>
	/// \if KO
	/// <para>현재 레이아웃에서 찾은 가상 키 버튼 시퀀스를 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores virtual-key buttons resolved from the current layout.</para>
	/// \endif
	/// </summary>
	protected IEnumerable<Key>? _keys;
	/// <summary>
	/// \if KO
	/// <para>언어 전환 버튼을 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the language-switch button.</para>
	/// \endif
	/// </summary>
	protected Button? _langBtn;
	/// <summary>
	/// \if KO
	/// <para>한영 입력 모드 버튼을 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the Korean/English input-mode button.</para>
	/// \endif
	/// </summary>
	protected Button? _inputModeBtn;

	/// <summary>
	/// \if KO
	/// <para>Backspace 반복 작업 취소 토큰 원본입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the cancellation-token source for Backspace repeat.</para>
	/// \endif
	/// </summary>
	private CancellationTokenSource? _repeatBackspaceCts;
	/// <summary>
	/// \if KO
	/// <para>마우스를 캡처한 반복 Backspace 키를 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the repeating Backspace key that captured the mouse.</para>
	/// \endif
	/// </summary>
	private Key? _repeatBackspaceKey;

	/// <summary>
	/// \if KO
	/// <para>컨트롤이 마지막으로 IME를 전환한 UTC 시각입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the UTC time at which the control last toggled the IME.</para>
	/// \endif
	/// </summary>
	private DateTime _lastSelfImeToggleAt = DateTime.MinValue;
	/// <summary>
	/// \if KO
	/// <para>컨트롤이 마지막으로 입력 언어를 전환한 UTC 시각입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the UTC time at which the control last switched the input language.</para>
	/// \endif
	/// </summary>
	private DateTime _lastSelfLangSwitchAt = DateTime.MinValue;
	/// <summary>
	/// \if KO
	/// <para>컨트롤이 마지막으로 Shift 입력을 시뮬레이션한 UTC 시각입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the UTC time at which the control last simulated Shift input.</para>
	/// \endif
	/// </summary>
	private DateTime _lastSelfShiftAt = DateTime.MinValue;
	/// <summary>
	/// \if KO
	/// <para>시스템 반영을 기다리는 한국어 입력 모드 값을 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the pending Korean input-mode value awaiting system synchronization.</para>
	/// \endif
	/// </summary>
	private bool? _pendingKoreanInputMode;
	/// <summary>
	/// \if KO
	/// <para>보류 중인 한국어 입력 모드를 유지할 UTC 기한입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the UTC deadline for retaining the pending Korean input mode.</para>
	/// \endif
	/// </summary>
	private DateTime _pendingKoreanInputModeUntil = DateTime.MinValue;
	/// <summary>
	/// \if KO
	/// <para>자체 생성 입력을 전역 후크에서 무시하는 보호 시간입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Defines the guard duration during which self-generated input is ignored by the global hook.</para>
	/// \endif
	/// </summary>
	private static readonly TimeSpan _guard = TimeSpan.FromMilliseconds(350);
	/// <summary>
	/// \if KO
	/// <para>현재 자체 IME 전환 보호 구간인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether the control is within its self-generated IME-toggle guard interval.</para>
	/// \endif
	/// </summary>
	private bool InImeGuard => (DateTime.UtcNow - _lastSelfImeToggleAt) < _guard;
	/// <summary>
	/// \if KO
	/// <para>현재 자체 언어 전환 보호 구간인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether the control is within its self-generated language-switch guard interval.</para>
	/// \endif
	/// </summary>
	private bool InLangGuard => (DateTime.UtcNow - _lastSelfLangSwitchAt) < _guard;
	/// <summary>
	/// \if KO
	/// <para>아직 유효한 한국어 입력 모드 변경이 보류 중인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether a still-valid Korean input-mode change is pending.</para>
	/// \endif
	/// </summary>
	private bool HasPendingKoreanInputMode => _pendingKoreanInputMode.HasValue && DateTime.UtcNow < _pendingKoreanInputModeUntil;
	// 문자 입력 시 대문자/기호를 위해 스스로 Shift를 눌렀다 떼는 동안, 전역 훅이
	// 그걸 실제 Shift 토글로 오인해 상태를 뒤집지 않도록 하는 가드.
	/// <summary>
	/// \if KO
	/// <para>현재 자체 Shift 입력 보호 구간인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether the control is within its self-generated Shift-input guard interval.</para>
	/// \endif
	/// </summary>
	private bool InShiftGuard => (DateTime.UtcNow - _lastSelfShiftAt) < _guard;

	/// <summary>
	/// \if KO
	/// <para>이 컨트롤의 관리 리소스가 해제되었는지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Indicates whether this control's managed resources have been disposed.</para>
	/// \endif
	/// </summary>
	private bool _disposed;

	#endregion

	#region Routed Event

	/// <summary>
	/// \if KO
	/// <para><see cref="VirtualKeyDown"/> 버블링 라우트 이벤트를 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the bubbling <see cref="VirtualKeyDown"/> routed event.</para>
	/// \endif
	/// </summary>
	public static readonly RoutedEvent VirtualKeyDownEvent = EventManager.
		RegisterRoutedEvent(nameof(VirtualKeyDown), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DreamineVirtualKeyboard));

	/// <summary>
	/// \if KO
	/// <para>가상 키가 눌렸을 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Occurs when a virtual key is pressed.</para>
	/// \endif
	/// </summary>
	public event RoutedEventHandler VirtualKeyDown
	{
		add { AddHandler(VirtualKeyDownEvent, value); }
		remove { RemoveHandler(VirtualKeyDownEvent, value); }
	}

	#endregion

	#region Dependency Property

	#region Layout

	/// <summary>
	/// \if KO
	/// <para><see cref="Layout"/> 종속성 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the <see cref="Layout"/> dependency property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty LayoutProperty =
		DependencyProperty.Register("Layout", typeof(VkLayout), typeof(DreamineVirtualKeyboard), new PropertyMetadata(VkLayout.Text, OnLayoutChanged));

	/// <summary>
	/// \if KO
	/// <para>표시할 가상 키보드 레이아웃을 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets the virtual-keyboard layout to display.</para>
	/// \endif
	/// </summary>
	public VkLayout Layout
	{
		get { return (VkLayout)GetValue(LayoutProperty); }
		set { SetValue(LayoutProperty, value); }
	}

	/// <summary>
	/// \if KO
	/// <para>레이아웃 변경 시 캐시된 시각 요소를 초기화하고 새 레이아웃을 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Clears cached visual elements and refreshes the new layout when the layout changes.</para>
	/// \endif
	/// </summary>
	/// <param name="d">
	/// \if KO
	/// <para>레이아웃이 변경된 키보드입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The keyboard whose layout changed.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이전 값과 새 레이아웃 값을 포함하는 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Event data containing the old and new layout values.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>읽기 전용 <see cref="IsPasswordLayout"/> 종속성 속성 키입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the key for the read-only <see cref="IsPasswordLayout"/> dependency property.</para>
	/// \endif
	/// </summary>
	private static readonly DependencyPropertyKey IsPasswordLayoutPropertyKey =
		DependencyProperty.RegisterReadOnly(nameof(IsPasswordLayout), typeof(bool), typeof(DreamineVirtualKeyboard), new PropertyMetadata(false, OnIsPasswordLayoutChanged));

	/// <summary>
	/// \if KO
	/// <para>읽기 전용 <see cref="IsPasswordLayout"/> 종속성 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the read-only <see cref="IsPasswordLayout"/> dependency property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty IsPasswordLayoutProperty = IsPasswordLayoutPropertyKey.DependencyProperty;

	/// <summary>
	/// \if KO
	/// <para>현재 레이아웃이 암호 입력용인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets whether the current layout is intended for password input.</para>
	/// \endif
	/// </summary>
	public bool IsPasswordLayout
	{
		get => (bool)GetValue(IsPasswordLayoutProperty);
		private set => SetValue(IsPasswordLayoutPropertyKey, value);
	}

	/// <summary>
	/// \if KO
	/// <para>암호 레이아웃 상태가 변경되면 미리 보기 요소의 표시 상태를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Refreshes preview-element visibility when password-layout state changes.</para>
	/// \endif
	/// </summary>
	/// <param name="d">
	/// \if KO
	/// <para>상태가 변경된 키보드입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The keyboard whose state changed.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>변경된 종속성 속성 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The dependency-property change data.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>현재 키보드 입력 언어를 가져옵니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets the current keyboard input language.</para>
	/// \endif
	/// </summary>
	public LanguageCode CurrentLang { get; private set; }
	/// <summary>
	/// \if KO
	/// <para>Shift 키가 눌린 상태인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets whether the Shift key is currently pressed.</para>
	/// \endif
	/// </summary>
	public bool IsPressedShift { get; private set; }

	/// <summary>
	/// \if KO
	/// <para>운영체제에서 읽은 실제 Caps Lock 토글 상태를 가져옵니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets the actual Caps Lock toggle state reported by the operating system.</para>
	/// \endif
	/// </summary>
	/// <remarks>
	/// \if KO
	/// <para>물리 키보드와 가상 키보드가 동일한 운영체제 상태를 사용하여 서로 다른 토글 상태를 만들지 않습니다.</para>
	/// \endif
	/// \if EN
	/// <para>Both physical and virtual keyboards use the same operating-system state to avoid conflicting toggle values.</para>
	/// \endif
	/// </remarks>
	public bool IsPressedCapsLock => Keyboard.IsKeyToggled(System.Windows.Input.Key.CapsLock);

	/// <summary>
	/// \if KO
	/// <para>Ctrl 키가 눌린 상태인지 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets whether the Ctrl key is currently pressed.</para>
	/// \endif
	/// </summary>
	public bool IsPressedCtrl { get; set; }
	/// <summary>
	/// \if KO
	/// <para>현재 IME가 활성 입력 모드인지 나타냅니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets whether the IME is currently in its active input mode.</para>
	/// \endif
	/// </summary>
	public bool ImeMode { get; private set; }

	#endregion

	#region Constructor

	/// <summary>
	/// \if KO
	/// <para><see cref="DreamineVirtualKeyboard"/> 클래스의 새 인스턴스를 초기화하고 상태 동기화와 전역 후크를 준비합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Initializes a new <see cref="DreamineVirtualKeyboard"/> instance and prepares state synchronization and global hooks.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>현재 시각적 트리에서 레이아웃 루트, 기능 버튼 및 키를 찾아 캐시합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Resolves and caches the layout root, function buttons, and keys from the current visual tree.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>전역 입력 후크 이벤트를 한 번만 구독하고 후크 실행 루프를 비동기로 시작합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Subscribes to global-input-hook events once and asynchronously starts the hook run loop.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>등록 시작이 완료될 때 끝나는 작업입니다.</para>
	/// \endif
	/// \if EN
	/// <para>A task that completes when registration has been initiated.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>실행 중인 전역 후크를 중지하고 등록된 이벤트 처리기를 해제합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stops the active global hook and unsubscribes its registered event handlers.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>현재 언어, IME 및 보조 키 상태를 모든 가상 키 표시에 반영합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Applies the current language, IME, and modifier states to every virtual-key display.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>현재 레이아웃에 대응하는 텍스트 또는 암호 미리 보기 컨트롤로 포커스를 이동합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Moves focus to the text or password preview control associated with the current layout.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>현재 정책에 따라 언어 전환 버튼을 숨깁니다.</para>
	/// \endif
	/// \if EN
	/// <para>Hides the language-switch button according to the current policy.</para>
	/// \endif
	/// </summary>
	protected void UpdateLangKey()
	{
		if (_langBtn != null)
		{
			_langBtn.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// \if KO
	/// <para>시스템 IME 상태와 입력 모드 버튼 표시를 동기화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Synchronizes the system IME state and the input-mode button display.</para>
	/// \endif
	/// </summary>
	/// <param name="readSystemIme">
	/// \if KO
	/// <para>시스템에서 최신 IME 상태를 읽을지 여부입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Whether to read the latest IME state from the system.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>IME 상태가 변경되었으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if the IME state changed; otherwise, <see langword="false"/>.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>입력 언어와 시각 요소를 다시 확인하고 전체 키보드 표시를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Re-evaluates the input language and visual elements, then refreshes the entire keyboard display.</para>
	/// \endif
	/// </summary>
	/// <param name="lang">
	/// \if KO
	/// <para>적용할 문화권 이름이며, 비어 있으면 현재 시스템 언어를 사용합니다.</para>
	/// \endif
	/// \if EN
	/// <para>The culture name to apply, or an empty string to use the current system language.</para>
	/// \endif
	/// </param>
	protected void RefreshKeyboardLayout(string lang = "")
	{
		SetLanguage(lang);
		FindElements();
		UpdateInputModeKey();
		UpdateKeys();
	}

	/// <summary>
	/// \if KO
	/// <para>타이머 틱마다 시스템 입력 언어와 IME 상태를 가상 키보드에 동기화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Synchronizes the system input language and IME state with the virtual keyboard on each timer tick.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>타이머 이벤트 원본입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The timer event source.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The event data.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>자모 키 입력을 한글 조합기로 처리하여 현재 미리 보기 텍스트에 반영합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Processes a jamo key through the Hangul composer and applies it to the current preview text.</para>
	/// \endif
	/// </summary>
	/// <param name="key">
	/// \if KO
	/// <para>처리할 가상 키입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The virtual key to process.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>조합 가능한 한글 입력으로 처리했으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if the key was handled as composable Hangul input.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>한글 조합 상태를 초기화하고 지정한 원시 텍스트를 현재 캐럿 위치에 삽입합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Resets Hangul composition and inserts the specified raw text at the current caret position.</para>
	/// \endif
	/// </summary>
	/// <param name="text">
	/// \if KO
	/// <para>삽입할 텍스트입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The text to insert.</para>
	/// \endif
	/// </param>
	private void InsertRawText(string text)
	{
		_hangulComposer.Reset();
		ReplaceTextTail(0, text);
	}

	/// <summary>
	/// \if KO
	/// <para>현재 선택 영역 또는 캐럿 앞의 지정된 문자 수를 새 텍스트로 교체합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Replaces the current selection or the specified number of characters before the caret with new text.</para>
	/// \endif
	/// </summary>
	/// <param name="replaceCount">
	/// \if KO
	/// <para>선택 영역이 없을 때 캐럿 앞에서 제거할 문자 수입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The number of characters to remove before the caret when there is no selection.</para>
	/// \endif
	/// </param>
	/// <param name="text">
	/// \if KO
	/// <para>삽입할 대체 텍스트입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The replacement text to insert.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Text Before Caret 값을 가져옵니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets the text before caret value.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>Get Text Before Caret 작업에서 생성한 <see cref="string"/> 결과입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> result produced by the get text before caret operation.</para>
	/// \endif
	/// </returns>
	private string GetTextBeforeCaret()
	{
		if (Layout == VkLayout.Password)
			return VkbPasswordBoxFromTree()?.Password ?? string.Empty;

		if (VkbTextBoxFromTree() is not { } textBox)
			return string.Empty;

		var caret = Math.Clamp(textBox.CaretIndex, 0, textBox.Text.Length);
		return textBox.Text[..caret];
	}

	/// <summary>
	/// \if KO
	/// <para>Password 값을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the password value.</para>
	/// \endif
	/// </summary>
	/// <param name="password">
	/// \if KO
	/// <para>password에 사용할 <see cref="string"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> value used for password.</para>
	/// \endif
	/// </param>
	private void SetPassword(string password)
	{
		if (VkbPasswordBoxFromTree() is { } passwordBox)
			passwordBox.Password = password;
	}

	/// <summary>
	/// \if KO
	/// <para>Vkb Text Box From Tree 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the vkb text box from tree operation.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>Vkb Text Box From Tree 작업에서 생성한 <see cref="TextBox"/> 결과입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="TextBox"/> result produced by the vkb text box from tree operation.</para>
	/// \endif
	/// </returns>
	private TextBox? VkbTextBoxFromTree()
	{
		var window = Window.GetWindow(this) ?? this as DependencyObject;
		return window == null ? null : FindByNameInVisualTree<TextBox>(window, "VkbTextBox");
	}

	/// <summary>
	/// \if KO
	/// <para>Vkb Password Box From Tree 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the vkb password box from tree operation.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>Vkb Password Box From Tree 작업에서 생성한 <see cref="PasswordBox"/> 결과입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="PasswordBox"/> result produced by the vkb password box from tree operation.</para>
	/// \endif
	/// </returns>
	private PasswordBox? VkbPasswordBoxFromTree()
	{
		var window = Window.GetWindow(this) ?? this as DependencyObject;
		return window == null ? null : FindByNameInVisualTree<PasswordBox>(window, "VkbPasswordBox");
	}

	/// <summary>
	/// \if KO
	/// <para>문자 키를 입력한다. Shift 토글/CapsLock 상태에 따라 물리 Shift를 한 글자만 감싸서 대문자/기호를 만든다. 상태는 클릭 핸들러가 직접 관리하므로 전역 훅이 꺼져 있어도(디버거 실행 등) 동작한다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the simulate char key operation.</para>
	/// \endif
	/// </summary>
	/// <param name="key">
	/// \if KO
	/// <para>key에 사용할 <see cref="Key"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="Key"/> value used for key.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Release Backspace Repeat 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the release backspace repeat operation.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>Start Backspace Repeat 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the start backspace repeat operation.</para>
	/// \endif
	/// </summary>
	/// <param name="key">
	/// \if KO
	/// <para>key에 사용할 <see cref="Key"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="Key"/> value used for key.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Simulate Backspace Stroke 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the simulate backspace stroke operation.</para>
	/// \endif
	/// </summary>
	private void SimulateBackspaceStroke()
	{
		_eventSimulator.SimulateKeyPress(KeyCode.VcBackspace);
		_eventSimulator.SimulateKeyRelease(KeyCode.VcBackspace);
	}

	/// <summary>
	/// \if KO
	/// <para>Normalize Language String 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the normalize language string operation.</para>
	/// \endif
	/// </summary>
	/// <param name="lang">
	/// \if KO
	/// <para>lang에 사용할 <see cref="string"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> value used for lang.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>Normalize Language String 작업에서 생성한 <see cref="string"/> 결과입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> result produced by the normalize language string operation.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>Language 값을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the language value.</para>
	/// \endif
	/// </summary>
	/// <param name="lang">
	/// \if KO
	/// <para>lang에 사용할 <see cref="string"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> value used for lang.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Korean English Mode 값을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the korean english mode value.</para>
	/// \endif
	/// </summary>
	/// <param name="useKorean">
	/// \if KO
	/// <para>use Korean에 사용할 <see cref="bool"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="bool"/> value used for use korean.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>Set Korean English Mode 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> when the set korean english mode condition is satisfied; otherwise, <see langword="false"/>.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>Apply Desired Ime Mode 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the apply desired ime mode operation.</para>
	/// \endif
	/// </summary>
	/// <param name="useKorean">
	/// \if KO
	/// <para>use Korean에 사용할 <see cref="bool"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="bool"/> value used for use korean.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Is Korean Input Active 조건을 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether is korean input active.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>Is Korean Input Active 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> when the is korean input active condition is satisfied; otherwise, <see langword="false"/>.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>Recheck Keyboard State Soon 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the recheck keyboard state soon operation.</para>
	/// \endif
	/// </summary>
	/// <param name="useKorean">
	/// \if KO
	/// <para>use Korean에 사용할 <see cref="bool"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="bool"/> value used for use korean.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Refresh Keyboard Visual State 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the refresh keyboard visual state operation.</para>
	/// \endif
	/// </summary>
	/// <param name="readSystemIme">
	/// \if KO
	/// <para>read System Ime에 사용할 <see cref="bool"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="bool"/> value used for read system ime.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Is Num Lock On 조건을 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether is num lock on.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>Is Num Lock On 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> when the is num lock on condition is satisfied; otherwise, <see langword="false"/>.</para>
	/// \endif
	/// </returns>
	private bool IsNumLockOn()
	{
		return Convert.ToBoolean(Win32Api.GetKeyState((int)KeyCode.VcNumLock) & 0x0001);
	}

	/// <summary>
	/// \if KO
	/// <para>Toggle Preview Visuals 작업을 시도하고 성공 여부를 반환합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Attempts to toggle preview visuals and returns whether the operation succeeds.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>By Name In Visual Tree 항목을 찾습니다.</para>
	/// \endif
	/// \if EN
	/// <para>Finds the by name in visual tree item.</para>
	/// \endif
	/// </summary>
	/// <typeparam name="T">
	/// \if KO
	/// <para>T 형식 매개변수입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The T type parameter.</para>
	/// \endif
	/// </typeparam>
	/// <param name="root">
	/// \if KO
	/// <para>root에 사용할 <see cref="DependencyObject"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="DependencyObject"/> value used for root.</para>
	/// \endif
	/// </param>
	/// <param name="name">
	/// \if KO
	/// <para>name에 사용할 <see cref="string"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="string"/> value used for name.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>Find By Name In Visual Tree 작업에서 생성한 <typeparamref name="T"/> 결과입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <typeparamref name="T"/> result produced by the find by name in visual tree operation.</para>
	/// \endif
	/// </returns>
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

	/// <summary>
	/// \if KO
	/// <para>Input Language Changed 이벤트 또는 상태 변경을 처리합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Handles the input language changed event or state change.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Keyboard User Control Loaded 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the keyboard user control loaded operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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
	/// \if KO
	/// <para>@brief Visual 트리에서 떨어질 때(창 닫힘 포함) 안전 정리.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the keyboard user control unloaded operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
	private void KeyboardUserControl_Unloaded(object? sender, RoutedEventArgs e)
	{
		_keyboardStateSyncTimer.Stop();
		Dispose(); // 안전
	}

	/// <summary>
	/// \if KO
	/// <para>Keyboard User Control Is Visible Changed 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the keyboard user control is visible changed operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Hook Key Pressed 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the hook key pressed operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Hook Key Released 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the hook key released operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>Hook Mouse Released 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the hook mouse released operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
	private void Hook_MouseReleased(object? sender, MouseHookEventArgs e)
	{
		ReleaseBackspaceRepeat();
	}

	/// <summary>
	/// \if KO
	/// <para>Backspace Pointer Released 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the backspace pointer released operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
	private void BackspacePointerReleased(object? sender, InputEventArgs e)
	{
		ReleaseBackspaceRepeat();
	}

	/// <summary>
	/// \if KO
	/// <para>Backspace Mouse Capture Lost 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the backspace mouse capture lost operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
	private void BackspaceMouseCaptureLost(object? sender, MouseEventArgs e)
	{
		if (_repeatBackspaceCts != null && _repeatBackspaceKey?.IsMouseCaptured != true)
			ReleaseBackspaceRepeat();
	}

	/// <summary>
	/// \if KO
	/// <para>Key Click 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the key click operation.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>이벤트를 발생시킨 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object that raised the event.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>이벤트와 관련된 데이터를 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Contains data associated with the event.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Releases resources owned by this instance.</para>
	/// \endif
	/// </summary>
	public void Dispose() => Dispose(true);

	/// <summary>
	/// \if KO
	/// <para>@brief Dispose 본체(표준 패턴).</para>
	/// \endif
	/// \if EN
	/// <para>Releases resources owned by this instance.</para>
	/// \endif
	/// </summary>
	/// <param name="disposing">
	/// \if KO
	/// <para>disposing에 사용할 <see cref="bool"/> 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The <see cref="bool"/> value used for disposing.</para>
	/// \endif
	/// </param>
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
	/// \if KO
	/// <para>@brief 파이널라이저(보호용).</para>
	/// \endif
	/// \if EN
	/// <para>Performs the ~dreamine virtual keyboard operation.</para>
	/// \endif
	/// </summary>
	~DreamineVirtualKeyboard() => Dispose(false);


	/// <summary>
	/// \if KO
	/// <para>Release Pressed Keys 작업을 수행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Performs the release pressed keys operation.</para>
	/// \endif
	/// </summary>
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

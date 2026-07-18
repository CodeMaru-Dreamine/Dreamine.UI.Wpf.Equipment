using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Controls.MessageBox;
namespace Dreamine.UI.Wpf.Equipment.Popup
{
	/// <summary>
	/// \if KO
	/// <para>팝업 닫기를 유발한 사용자 또는 시스템 동작을 구분합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the user or system action that initiated popup closure.</para>
	/// \endif
	/// </summary>
	internal enum PopupAction
	{
		/// <summary>
		/// \if KO
		/// <para>아직 닫기 동작이 없습니다.</para>
		/// \endif
		/// \if EN
		/// <para>No close action has occurred.</para>
		/// \endif
		/// </summary>
		None,

		/// <summary>
		/// \if KO
		/// <para>확인 버튼으로 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closure was requested by the OK button.</para>
		/// \endif
		/// </summary>
		Ok,

		/// <summary>
		/// \if KO
		/// <para>취소 버튼으로 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closure was requested by the cancel button.</para>
		/// \endif
		/// </summary>
		Cancel,

		/// <summary>
		/// \if KO
		/// <para>창 닫기 또는 Alt+F4 같은 시스템 동작으로 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closure was requested by a system action such as the close button or Alt+F4.</para>
		/// \endif
		/// </summary>
		SystemClose
	}

	/// <summary>
	/// \if KO
	/// <para>점멸 애니메이션, 결과 전달 및 선택적 시스템 닫기 차단을 제공하는 팝업 창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Represents a popup window with blink animation, result propagation, and optional system-close blocking.</para>
	/// \endif
	/// </summary>
	public partial class DreamineBlinkPopupWindow : Window
	{
		/// <summary>
		/// \if KO
		/// <para>창 생성 시 전달된 팝업 옵션을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores popup options supplied when the window was created.</para>
		/// \endif
		/// </summary>
		private readonly BlinkPopupOptions _opt;

		/// <summary>
		/// \if KO
		/// <para>배경 색상 및 불투명도 점멸 스토리보드를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the background color and opacity blink storyboard.</para>
		/// \endif
		/// </summary>
		private Storyboard? _blinkSb;

		/// <summary>
		/// \if KO
		/// <para>확인, 취소 또는 시스템 닫기로 요청된 결과를 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the requested result for OK, cancel, or system closure.</para>
		/// \endif
		/// </summary>
		private bool? _requestedResult = null;

		/// <summary>
		/// \if KO
		/// <para>비모달 <see cref="Window.Show"/>로 열린 팝업의 결과를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the result of a popup opened modelessly through <see cref="Window.Show"/>.</para>
		/// \endif
		/// </summary>
		public bool? PopupResult => _requestedResult;

		/// <summary>
		/// \if KO
		/// <para>닫힘 처리 재진입을 방지하는 상태입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the state that prevents reentrant closing logic.</para>
		/// \endif
		/// </summary>
		private bool _inClosing = false;

		/// <summary>
		/// \if KO
		/// <para>마지막 닫기 시도 동작을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the action used for the last close attempt.</para>
		/// \endif
		/// </summary>
		private PopupAction _lastAction = PopupAction.None;

		/// <summary>
		/// \if KO
		/// <para>Win32 시스템 명령 메시지를 후킹할 창 원본을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the window source used to hook Win32 system-command messages.</para>
		/// \endif
		/// </summary>
		private HwndSource? _hwndSrc;

		/// <summary>
		/// \if KO
		/// <para>Win32 시스템 명령 메시지 식별자입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the Win32 system-command message identifier.</para>
		/// \endif
		/// </summary>
		private const int WM_SYSCOMMAND = 0x0112;
		/// <summary>
		/// \if KO
		/// <para>Win32 창 닫기 시스템 명령입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies the Win32 close-window system command.</para>
		/// \endif
		/// </summary>
		private const int SC_CLOSE = 0xF060;

		/// <summary>
		/// \if KO
		/// <para>옵션에서 뷰 모델, 창 모드, 크기, 제목 및 최상위 상태를 초기화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes the view model, window mode, size, title, and topmost state from options.</para>
		/// \endif
		/// </summary>
		/// <param name="opt">
		/// \if KO
		/// <para>팝업 표시 및 동작 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Popup display and behavior options.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="opt"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="opt"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public DreamineBlinkPopupWindow(BlinkPopupOptions opt)
		{
			InitializeComponent();
			_opt = opt;

			// ViewModel 생성 및 닫힘 이벤트 연결
			var vm = new DreamineBlinkPopupWindowViewModel(opt);
			vm.CloseRequested += (_, dr) =>
			{
				_requestedResult = dr;
				_lastAction = dr == true
					? PopupAction.Ok
					: dr == false
						? PopupAction.Cancel
						: PopupAction.SystemClose;

				RequestCloseNextTick();
			};
			DataContext = vm;

			// TopMost / 기본 타이틀 적용
			Topmost = _opt.TopMost;
			if (!string.IsNullOrEmpty(_opt.Title))
			{
				Title = _opt.Title;
			}

			// 윈도우 크기 / 모드 설정
			if (_opt.Fullscreen)
			{
				WindowState = WindowState.Maximized;
			}
			else
			{
				WindowState = WindowState.Normal;

				if (_opt.FixedSize is Size sz)
				{
					Width = sz.Width;
					Height = sz.Height;
				}
				else
				{
					SizeToContent = SizeToContent.WidthAndHeight;
				}
			}
		}

		/// <summary>
		/// \if KO
		/// <para>콘텐츠 렌더링 후 옵션이 활성화되어 있으면 점멸 애니메이션을 시작합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Starts blink animation after content rendering when enabled by options.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>콘텐츠 렌더링 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Content-rendered event data.</para>
		/// \endif
		/// </param>
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			if (_opt.UseBlink)
			{
				StartBlink();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>닫기 동작을 기록하고 요청된 대화 결과를 적용하면서 재진입을 방지합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Records the close action, applies the requested dialog result, and prevents reentrant closing.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>닫기 취소 상태를 포함하는 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closing event data containing cancellation state.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 기본 구현에서 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown by the base implementation when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (_lastAction == PopupAction.None)
				_lastAction = PopupAction.SystemClose;

			if (_inClosing)
			{
				base.OnClosing(e);
				return;
			}
			_inClosing = true;

			try
			{
				if (_requestedResult.HasValue)
				{
					try { DialogResult = _requestedResult; }
					catch (InvalidOperationException) { }
				}

				base.OnClosing(e);
			}
			finally
			{
				_inClosing = false;
			}
		}

		/// <summary>
		/// \if KO
		/// <para>네이티브 창 원본을 확인하고 시스템 명령 메시지 후크를 등록합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Resolves the native window source and installs the system-command message hook.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>원본 초기화 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Source-initialized event data.</para>
		/// \endif
		/// </param>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			_hwndSrc = PresentationSource.FromVisual(this) as HwndSource;
			_hwndSrc?.AddHook(WndProcHook);
		}

		/// <summary>
		/// \if KO
		/// <para>옵션이 설정되면 Alt+F4 키 조합을 처리하여 창 닫기를 차단합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Handles the Alt+F4 key combination to block closure when configured.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>미리보기 키 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Preview key-event data.</para>
		/// \endif
		/// </param>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (_opt.BlockAltF4 &&
				e.SystemKey == Key.F4 &&
				(Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
			{
				e.Handled = true;
				return;
			}

			base.OnPreviewKeyDown(e);
		}

		/// <summary>
		/// \if KO
		/// <para>옵션이 설정된 경우 Win32 창 닫기 시스템 명령을 처리됨으로 표시합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Marks the Win32 close-window system command as handled when configured.</para>
		/// \endif
		/// </summary>
		/// <param name="hwnd">
		/// \if KO
		/// <para>메시지를 받은 창 핸들입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The handle of the receiving window.</para>
		/// \endif
		/// </param>
		/// <param name="msg">
		/// \if KO
		/// <para>Win32 메시지 ID입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The Win32 message identifier.</para>
		/// \endif
		/// </param>
		/// <param name="wParam">
		/// \if KO
		/// <para>메시지 명령 매개변수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The message command parameter.</para>
		/// \endif
		/// </param>
		/// <param name="lParam">
		/// \if KO
		/// <para>추가 메시지 매개변수입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The additional message parameter.</para>
		/// \endif
		/// </param>
		/// <param name="handled">
		/// \if KO
		/// <para>처리 여부 출력값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The handled-state output.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>후크 처리 결과로 항상 0입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The hook result, always zero.</para>
		/// \endif
		/// </returns>
		private IntPtr WndProcHook(
			IntPtr hwnd,
			int msg,
			IntPtr wParam,
			IntPtr lParam,
			ref bool handled)
		{
			if (_opt.BlockAltF4 &&
				msg == WM_SYSCOMMAND &&
				wParam.ToInt32() == SC_CLOSE)
			{
				handled = true;
				return IntPtr.Zero;
			}

			return IntPtr.Zero;
		}

		/// <summary>
		/// \if KO
		/// <para>애플리케이션 유휴 디스패처 틱에서 창 닫기를 시도하고 실패하면 한 번 재시도합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attempts to close the window on an application-idle dispatcher tick and retries once on failure.</para>
		/// \endif
		/// </summary>
		private void RequestCloseNextTick()
		{
			if (!IsLoaded)
			{
				return;
			}

			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
			{
				Interval = TimeSpan.FromMilliseconds(1)
			};

			timer.Tick += (_, __) =>
			{
				timer.Stop();

				if (!IsLoaded)
				{
					return;
				}

				try
				{
					Close();
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[Popup] Close failed, retrying: {ex.Message}");
					Dispatcher.BeginInvoke(
						() =>
						{
							try { if (IsLoaded) Close(); }
							catch (Exception retryEx) { Debug.WriteLine($"[Popup] Close retry failed: {retryEx.Message}"); }
						},
						DispatcherPriority.ContextIdle);
				}
			};

			timer.Start();
		}

		/// <summary>
		/// \if KO
		/// <para>창이 닫힌 뒤 점멸 애니메이션과 Win32 후크를 정리합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Cleans up blink animation and the Win32 hook after the window closes.</para>
		/// \endif
		/// </summary>
		/// <param name="e">
		/// \if KO
		/// <para>닫힘 이벤트 데이터입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closed-event data.</para>
		/// \endif
		/// </param>
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			StopBlink();
		}

		/// <summary>
		/// \if KO
		/// <para>옵션의 색상·불투명도·간격·횟수로 배경 점멸 스토리보드를 만들고 시작합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Creates and starts a background blink storyboard from option colors, opacities, interval, and count.</para>
		/// \endif
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// \if KO
		/// <para>창 리소스에 <c>RootBrush</c>가 없을 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when the window resources do not contain <c>RootBrush</c>.</para>
		/// \endif
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// \if KO
		/// <para>점멸 간격 또는 반복 횟수가 애니메이션에서 허용되지 않을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when the blink interval or repeat count is invalid for animation.</para>
		/// \endif
		/// </exception>
		private void StartBlink()
		{
			var rootBrushObj = Resources["RootBrush"];
			SolidColorBrush brush =
				rootBrushObj is SolidColorBrush scb
					? (scb.IsFrozen ? scb.Clone() : scb)
					: new SolidColorBrush(_opt.Color1);

			Resources["RootBrush"] = brush;

			var colorAnim = new ColorAnimationUsingKeyFrames();
			colorAnim.KeyFrames.Add(
				new DiscreteColorKeyFrame(
					_opt.Color1,
					KeyTime.FromTimeSpan(TimeSpan.Zero)));
			colorAnim.KeyFrames.Add(
				new DiscreteColorKeyFrame(
					_opt.Color2,
					KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(_opt.BlinkIntervalMs))));
			colorAnim.KeyFrames.Add(
				new DiscreteColorKeyFrame(
					_opt.Color1,
					KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(_opt.BlinkIntervalMs * 2))));

			Storyboard.SetTarget(colorAnim, brush);
			Storyboard.SetTargetProperty(colorAnim, new PropertyPath(SolidColorBrush.ColorProperty));

			var opAnim = new DoubleAnimationUsingKeyFrames();
			opAnim.KeyFrames.Add(
				new DiscreteDoubleKeyFrame(
					_opt.Opacity1,
					KeyTime.FromTimeSpan(TimeSpan.Zero)));
			opAnim.KeyFrames.Add(
				new DiscreteDoubleKeyFrame(
					_opt.Opacity2,
					KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(_opt.BlinkIntervalMs))));
			opAnim.KeyFrames.Add(
				new DiscreteDoubleKeyFrame(
					_opt.Opacity1,
					KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(_opt.BlinkIntervalMs * 2))));

			Storyboard.SetTarget(opAnim, RootBorder);
			Storyboard.SetTargetProperty(opAnim, new PropertyPath(OpacityProperty));

			_blinkSb = new Storyboard
			{
				RepeatBehavior = _opt.BlinkRepeatCount <= 0
					? RepeatBehavior.Forever
					: new RepeatBehavior(_opt.BlinkRepeatCount)
			};
			_blinkSb.Children.Add(colorAnim);
			_blinkSb.Children.Add(opAnim);
			_blinkSb.Begin(this, true);
		}

		/// <summary>
		/// \if KO
		/// <para>진행 중인 점멸 스토리보드를 중지하고 Win32 메시지 후크를 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stops the active blink storyboard and removes the Win32 message hook.</para>
		/// \endif
		/// </summary>
		private void StopBlink()
		{
			_blinkSb?.Stop(this);
			_blinkSb = null;

			_hwndSrc?.RemoveHook(WndProcHook);
			_hwndSrc = null;
		}
	}
}

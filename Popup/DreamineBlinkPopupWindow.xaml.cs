using System;
using System.ComponentModel;
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
	/// <summary>\enum PopupAction \brief 팝업 닫힘 유발 동작 구분.</summary>
	internal enum PopupAction
	{
		/// <summary>\brief 아무 동작 없음. </summary>
		None,

		/// <summary>\brief OK 버튼 클릭. </summary>
		Ok,

		/// <summary>\brief Cancel 버튼 클릭. </summary>
		Cancel,

		/// <summary>\brief 시스템 닫기(창 x / Alt+F4 등). </summary>
		SystemClose
	}

	/// <summary>
	/// \class DreamineBlinkPopupWindow
	/// \brief BlinkPopupWindow의 코드비하인드 클래스.
	/// \details
	///  - BlinkPopupOptions를 기반으로 ViewModel을 생성하고 DataContext에 주입합니다.
	///  - OK/Cancel 버튼 클릭 시 CloseRequested 이벤트를 통해 닫기 요청을 처리합니다.
	///  - Alt+F4 및 WM_SYSCOMMAND(SC_CLOSE)를 차단하는 옵션(BlockAltF4)을 지원합니다.
	///  - 권한 인증(로그인)이 필요한 경우, 닫기 직전에 LoginDialog를 호출하여 인증을 수행합니다.
	/// </details>
	public partial class DreamineBlinkPopupWindow : Window
	{
		/// <summary>\brief 생성 시 전달된 팝업 옵션. </summary>
		private readonly BlinkPopupOptions _opt;

		/// <summary>\brief 배경 깜빡임에 사용되는 스토리보드. </summary>
		private Storyboard? _blinkSb;

		/// <summary>\brief 사용자가 요청한 DialogResult(OK=true, Cancel=false, SystemClose=null). </summary>
		private bool? _requestedResult = null;

		/// <summary>\brief Closing 재진입 방지 플래그. </summary>
		private bool _inClosing = false;

		/// <summary>\brief 마지막으로 어떤 동작으로 닫기를 시도했는지 기록. </summary>
		private PopupAction _lastAction = PopupAction.None;

		/// <summary>\brief Win32 메시지 후킹을 위한 HwndSource. </summary>
		private HwndSource? _hwndSrc;

		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_CLOSE = 0xF060;

		/// <summary>
		/// \brief DreamineBlinkPopupWindow 생성자.
		/// \Param opt 팝업 옵션.
		/// </summary>
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

		/// <summary>\brief 컨텐츠 렌더 완료 시점에 깜빡임 애니메이션을 시작합니다.</summary>
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			if (_opt.UseBlink)
			{
				StartBlink();
			}
		}

		/// <summary>
		/// \brief 창 닫기 직전 훅.
		/// \details
		///  - OK/Cancel/SystemClose 동작에 대해 권한 인증이 필요한지 판정합니다.
		///  - 인증이 필요하면 일단 닫기를 막고(e.Cancel = true), 다음 틱에서 LoginDialog를 띄웁니다.
		///  - 이미 인증을 통과했거나, 인증이 필요 없는 경우 DialogResult를 반영하고 그대로 닫습니다.
		/// </details>
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
					DialogResult = _requestedResult;

				base.OnClosing(e);
			}
			finally
			{
				_inClosing = false;
			}
		}

		/// <summary>\brief Win32 메시지 훅 등록.</summary>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			_hwndSrc = PresentationSource.FromVisual(this) as HwndSource;
			_hwndSrc?.AddHook(WndProcHook);
		}

		/// <summary>\brief Alt+F4 SystemKey 선제 차단.</summary>
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

		/// <summary>\brief Win32 메시지 훅(Alt+F4/SC_CLOSE 차단).</summary>
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

		/// <summary>\brief 안전한 CloseAsync 트리거(다음 틱에서 CloseAsync 호출).</summary>
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
				catch
				{
					Dispatcher.BeginInvoke(
						() =>
						{
							if (IsLoaded)
							{
								Close();
							}
						},
						DispatcherPriority.ContextIdle);
				}
			};

			timer.Start();
		}

		/// <summary>\brief 창이 완전히 닫힐 때 깜빡임 애니메이션과 훅을 정리합니다.</summary>
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			StopBlink();
		}

		/// <summary>\brief 깜빡임(색상/불투명도) 애니메이션을 시작합니다.</summary>
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

		/// <summary>\brief 깜빡임 애니메이션과 Win32 훅을 정지/해제합니다.</summary>
		private void StopBlink()
		{
			_blinkSb?.Stop(this);
			_blinkSb = null;

			_hwndSrc?.RemoveHook(WndProcHook);
			_hwndSrc = null;
		}
	}
}

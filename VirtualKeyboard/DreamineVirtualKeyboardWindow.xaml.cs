using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>배치 대상에 맞춰 위치를 조정하고 값 확정·취소·검증을 처리하는 가상 키보드 창입니다.</para>
/// \endif
/// \if EN
/// <para>Represents a virtual-keyboard window that positions itself near a target and handles commit, cancel, and validation.</para>
/// \endif
/// </summary>
public partial class DreamineVirtualKeyboardWindow : Window
{
	/// <summary>
	/// \if KO
	/// <para>키보드가 현재 편집하는 배치 대상 요소를 보관합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the placement target currently edited by the keyboard.</para>
	/// \endif
	/// </summary>
	private DependencyObject? _placementTarget;

	/// <summary>
	/// \if KO
	/// <para>XAML 구성 요소와 창·입력·애플리케이션 수명 이벤트를 등록합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Initializes XAML components and registers window, input, and application lifetime events.</para>
	/// \endif
	/// </summary>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para>현재 WPF 애플리케이션이 없을 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when no current WPF application exists.</para>
	/// \endif
	/// </exception>
	public DreamineVirtualKeyboardWindow()
	{
		InitializeComponent();
		SizeChanged += DreamineVirtualKeyboardWindow_SizeChanged;
		LocationChanged += DreamineVirtualKeyboardWindow_LocationChanged;
		IsVisibleChanged += DreamineVirtualKeyboardWindow_IsVisibleChanged;
		MouseDown += DreamineVirtualKeyboardWindow_MouseDown;
		KeyUp += DreamineVirtualKeyboardWindow_KeyUp;
		Closing += DreamineVirtualKeyboardWindow_Closing;
		System.Windows.Application.Current.Exit += Application_Exit;
	}

	/// <summary>
	/// \if KO
	/// <para>창 크기 변경 시 배치 대상 기준 위치를 다시 계산합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Recalculates target-relative placement when the window size changes.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>크기 변경 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Size-change data.</para>
	/// \endif
	/// </param>
	private void DreamineVirtualKeyboardWindow_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		CalcKeyboardPosition();
	}

	/// <summary>
	/// \if KO
	/// <para>창 위치 변경 후 텍스트 입력 포커스를 복원합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Restores text-input focus after the window location changes.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>위치 변경 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Location-change data.</para>
	/// \endif
	/// </param>
	private void DreamineVirtualKeyboardWindow_LocationChanged(object? sender, EventArgs e)
	{
		VirtualKeyboardComp.FocusVkbTextBox();
	}

	/// <summary>
	/// \if KO
	/// <para>창 표시 상태에 따라 입력 핸들러·바인딩·배치 대상을 정리하거나 위치를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Cleans up input handlers, binding, and target or updates placement according to window visibility.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>새 표시 상태입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The new visibility state.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidCastException">
	/// \if KO
	/// <para>새 값이 <see cref="bool"/>이 아닐 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when the new value is not a <see cref="bool"/>.</para>
	/// \endif
	/// </exception>
	private void DreamineVirtualKeyboardWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		var isVisible = (bool)e.NewValue;
		if (!isVisible)
		{
			DreamineVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);
			VirtualKeyboardComp.ReleasePressedKeys();
			VirtualKeyboardComp.RemoveNumericHandler();
			BindingOperations.ClearBinding(VirtualKeyboardComp, DreamineVirtualKeyboardUI.ValueProperty);
			ClearText();
			SetPlacementTarget(null);
		}
		else
		{
			if (IsLoaded)
			{
				CalcKeyboardPosition();
			}
		}
	}

	/// <summary>
	/// \if KO
	/// <para>왼쪽 마우스 버튼을 누르면 창 끌기를 시작합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Begins dragging the window when the left mouse button is pressed.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>마우스 입력 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Mouse-input data.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// \if KO
	/// <para>왼쪽 버튼이 눌리지 않은 상태로 끌기를 시작할 때 WPF에서 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown by WPF if dragging starts without a valid left-button state.</para>
	/// \endif
	/// </exception>
	private void DreamineVirtualKeyboardWindow_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
			this.DragMove();
	}

	/// <summary>
	/// \if KO
	/// <para>키 놓기 동작을 비동기로 처리하고 예외를 진단 출력에 기록합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Handles key release asynchronously and writes exceptions to diagnostic output.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>입력 이벤트 발신자입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The input-event source.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>키 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Key-event data.</para>
	/// \endif
	/// </param>
	/// <remarks>
	/// \if KO
	/// <para><see langword="async void"/> WPF 이벤트 처리기입니다.</para>
	/// \endif
	/// \if EN
	/// <para>This is an <see langword="async void"/> WPF event handler.</para>
	/// \endif
	/// </remarks>
	private async void DreamineVirtualKeyboardWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		try { await HandleKeyUpAsync(e); }
		catch (Exception ex) { Debug.WriteLine($"[VK] KeyUp error: {ex.Message}"); }
	}

	/// <summary>
	/// \if KO
	/// <para>Enter 키는 값과 공급자 동작을 확정하고 Escape 키는 취소한 뒤 창을 숨깁니다.</para>
	/// \endif
	/// \if EN
	/// <para>Commits value and provider actions for Enter or cancels for Escape before hiding the window.</para>
	/// \endif
	/// </summary>
	/// <param name="e">
	/// \if KO
	/// <para>처리할 키 이벤트입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The key event to handle.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>비동기 처리 작업입니다.</para>
	/// \endif
	/// \if EN
	/// <para>A task representing asynchronous handling.</para>
	/// \endif
	/// </returns>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	private async Task HandleKeyUpAsync(System.Windows.Input.KeyEventArgs e)
	{
		switch (e.Key)
		{
			case System.Windows.Input.Key.Enter:
				UpdateSourceBinding();

				var result = await InvokeProvdersAsync();
				if (!result)
				{
					return;
				}

				FocusPlacementTarget();
				ClearText();
				await Task.Delay(100);
				if (IsVisible) Hide();
				break;
			case System.Windows.Input.Key.Escape:
				FocusPlacementTarget();
				ClearText();
				await Task.Delay(100);
				if (IsVisible) Hide();
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// \if KO
	/// <para>배치 대상에 등록된 Enter 동작 공급자를 순서대로 실행하고 첫 거부에서 중단합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Executes Enter-action providers registered for the target in order and stops at the first rejection.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>모든 공급자가 승인하면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if every provider accepts.</para>
	/// \endif
	/// </returns>
	private async Task<bool> InvokeProvdersAsync()
	{
		if (_placementTarget != null)
		{
			var providers = DreamineVirtualKeyboardAssist.GetEnterActionProviders(_placementTarget);
			foreach (var provider in providers)
			{
				var result = await provider.ExecuteAsync();
				result.Show(VirtualKeyboardComp.MsgTbl);
				if (!result.IsAccepted())
				{
					if (provider.PlacementTarget != null && DreamineVirtualKeyboardAssist.TargetChanged(provider.PlacementTarget))
					{
						DreamineVirtualKeyboardAssist.ShowDreamineVirtualKeyboard(provider.PlacementTarget);
					}

					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// \if KO
	/// <para>창 닫기 시 아이콘, 키보드 리소스 및 공유 창 상태를 정리합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Cleans up the icon, keyboard resources, and shared window state when closing.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>닫기 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Closing-event data.</para>
	/// \endif
	/// </param>
	private void DreamineVirtualKeyboardWindow_Closing(object? sender, CancelEventArgs e)
	{
		DreamineVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);
		VirtualKeyboardComp?.Dispose();
		DreamineVirtualKeyboardAssist.ResetDreamineVirtualKeyboard();
	}

	/// <summary>
	/// \if KO
	/// <para>애플리케이션 종료 시 가상 키보드 리소스를 해제합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Disposes virtual-keyboard resources when the application exits.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>애플리케이션입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The application.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>종료 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Exit-event data.</para>
	/// \endif
	/// </param>
	private void Application_Exit(object sender, ExitEventArgs e)
	{
		VirtualKeyboardComp?.Dispose();
	}


	/// <summary>
	/// \if KO
	/// <para>배치 대상과 모니터 작업 영역을 기준으로 키보드 창 위치를 계산하고 화면 안으로 보정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Calculates keyboard-window placement from the target and monitor work area, then shifts it onscreen.</para>
	/// \endif
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// \if KO
	/// <para>배치 대상이 PresentationSource에 연결되지 않은 경우 화면 좌표 변환에서 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown during screen-coordinate conversion when the target is not connected to a presentation source.</para>
	/// \endif
	/// </exception>
	private void CalcKeyboardPosition()
	{
		if (_placementTarget is not UIElement ui)
			return;

		Point ptDev = ui.PointToScreen(new Point(0, 0));

		var ps = PresentationSource.FromVisual(ui);
		Matrix fromDevice = ps?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

		Point ptDip = fromDevice.Transform(ptDev);

		var hMonitor = NativeMethods.MonitorFromPoint(new NativeMethods.POINT { x = (int)ptDev.X, y = (int)ptDev.Y }, NativeMethods.MONITOR_DEFAULTTONEAREST);
		var mi = new NativeMethods.MONITORINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFO>() };
		NativeMethods.GetMonitorInfo(hMonitor, ref mi);
		var waR = mi.rcWork;
		Point waTopLeftDip = fromDevice.Transform(new Point(waR.left, waR.top));
		Point waBottomRightDip = fromDevice.Transform(new Point(waR.right, waR.bottom));
		var waDip = new Rect(waTopLeftDip, waBottomRightDip);

		double windowHeight = ActualHeight;
		double windowWidth = ActualWidth;
		if (windowHeight <= 0 || windowWidth <= 0) return;

		double desiredTop;
		if (ptDip.Y + ui.RenderSize.Height + windowHeight > waDip.Bottom)
		{
			desiredTop = Math.Max(ptDip.Y - windowHeight, waDip.Top);
		}
		else
		{
			desiredTop = ptDip.Y + ui.RenderSize.Height;
		}

		double desiredLeft = Math.Min(Math.Max(ptDip.X, waDip.Left), waDip.Right - windowWidth);

		Left = desiredLeft;
		Top = desiredTop;

		ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this, waDip);
	}


	/// <summary>
	/// \if KO
	/// <para>키보드 레이아웃을 설정하고 창 크기, 기본 범위 및 표시 상태를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the keyboard layout and updates window size, default range, and visibility state.</para>
	/// \endif
	/// </summary>
	/// <param name="layout">
	/// \if KO
	/// <para>적용할 키보드 레이아웃입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The keyboard layout to apply.</para>
	/// \endif
	/// </param>
	public void SetLayout(VkLayout layout)
	{
		VirtualKeyboardComp.Layout = layout;

		SetDisplayKeyboardSize(layout);

		SetDefaultMinMaxValue(layout);

		UpdateVisibilityState();
	}

	/// <summary>
	/// \if KO
	/// <para>텍스트 계열과 숫자 계열 레이아웃에 맞는 최대 창 너비를 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets maximum window width for text-family or numeric-family layouts.</para>
	/// \endif
	/// </summary>
	/// <param name="layout">
	/// \if KO
	/// <para>기준 레이아웃입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The layout used for sizing.</para>
	/// \endif
	/// </param>
	private void SetDisplayKeyboardSize(VkLayout layout)
	{
		MaxWidth = layout == VkLayout.Text || layout == VkLayout.Password ? 948 : 548;
	}

	/// <summary>
	/// \if KO
	/// <para>소수 또는 정수 레이아웃에 맞는 기본 최소·최대값을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets default minimum and maximum values for decimal or integer layouts.</para>
	/// \endif
	/// </summary>
	/// <param name="layout">
	/// \if KO
	/// <para>기준 레이아웃입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The layout used for defaults.</para>
	/// \endif
	/// </param>
	private void SetDefaultMinMaxValue(VkLayout layout)
	{
		if (layout == VkLayout.Decimal)
		{
			VirtualKeyboardComp.Minimum = decimal.MinValue;
			VirtualKeyboardComp.Maximum = decimal.MaxValue;
		}
		else
		{
			VirtualKeyboardComp.Minimum = int.MinValue;
			VirtualKeyboardComp.Maximum = int.MaxValue;
		}
	}

	/// <summary>
	/// \if KO
	/// <para>배치 대상 값을 키보드에 바인딩하고 숫자 범위와 소수 표시 형식을 구성합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Binds the target value to the keyboard and configures numeric range and decimal display format.</para>
	/// \endif
	/// </summary>
	/// <param name="binding">
	/// \if KO
	/// <para>값에 적용할 WPF 바인딩입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The WPF binding applied to the value.</para>
	/// \endif
	/// </param>
	/// <param name="min">
	/// \if KO
	/// <para>최소 숫자 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The minimum numeric value.</para>
	/// \endif
	/// </param>
	/// <param name="max">
	/// \if KO
	/// <para>최대 숫자 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The maximum numeric value.</para>
	/// \endif
	/// </param>
	/// <param name="decimalFormat">
	/// \if KO
	/// <para>선택적 소수 형식 문자열입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The optional decimal format string.</para>
	/// \endif
	/// </param>
	public void SetBinding(System.Windows.Data.Binding binding, decimal min = decimal.MinValue, decimal max = decimal.MaxValue, string decimalFormat = "")
	{
		if (binding == null) return;
		if (_placementTarget is PasswordBox passwordBox)
		{
			VirtualKeyboardComp.VkbPasswordBox.Password = passwordBox.Password;
		}
		else
		{
			BindingOperations.SetBinding(VirtualKeyboardComp, DreamineVirtualKeyboardUI.ValueProperty, binding);
			VirtualKeyboardComp.Minimum = min;
			VirtualKeyboardComp.Maximum = max;
			VirtualKeyboardComp.DecimalFormat = decimalFormat;
		}

		UpdateVisibilityState();
		VirtualKeyboardComp.AddOrRemoveNumericHandler();
	}

	/// <summary>
	/// \if KO
	/// <para>현재 배치 대상을 설정하고 암호·일반 입력 UI, 창 위치 및 소유 창을 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the current placement target and updates password or text UI, window placement, and ownership.</para>
	/// \endif
	/// </summary>
	/// <param name="placementTarget">
	/// \if KO
	/// <para>편집할 대상이며 정리 시 <see langword="null"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The target to edit, or <see langword="null"/> during cleanup.</para>
	/// \endif
	/// </param>
	public void SetPlacementTarget(DependencyObject? placementTarget)
	{
		DreamineVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);

		_placementTarget = placementTarget;

		if (_placementTarget is PasswordBox passwordBox)
		{
			VirtualKeyboardComp.VkbPasswordBox.Visibility = Visibility.Visible;
			VirtualKeyboardComp.VkbTextBox.Visibility = Visibility.Collapsed;
		}
		else
		{
			VirtualKeyboardComp.VkbPasswordBox.Visibility = Visibility.Collapsed;
			VirtualKeyboardComp.VkbTextBox.Visibility = Visibility.Visible;
		}

		if (IsLoaded)
		{
			CalcKeyboardPosition();
		}

		UpdateWindowOwner();
	}

	/// <summary>
	/// \if KO
	/// <para>배치 대상이 속한 창을 가상 키보드의 소유 창으로 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the window containing the placement target as the virtual keyboard's owner.</para>
	/// \endif
	/// </summary>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para>대상이 창에 연결되어 있지 않아 <see cref="Window.GetWindow(DependencyObject)"/>가 null을 반환할 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when <see cref="Window.GetWindow(DependencyObject)"/> returns null for a detached target.</para>
	/// \endif
	/// </exception>
	private void UpdateWindowOwner()
	{
		if (_placementTarget != null)
		{
			var ownerWindow = Window.GetWindow(_placementTarget);
			ownerWindow.Closing -= OwnerWindow_Closing;
			ownerWindow.Closing += OwnerWindow_Closing;
			SetWindowOwner(ownerWindow);
		}
		else
		{
			SetWindowOwner(null);
		}
	}

	/// <summary>
	/// \if KO
	/// <para>소유 창이 닫힐 때 소유 관계를 제거하고 표시 중인 키보드를 숨깁니다.</para>
	/// \endif
	/// \if EN
	/// <para>Clears ownership and hides the visible keyboard when the owner closes.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>닫히는 소유 창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The closing owner window.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>닫기 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Closing-event data.</para>
	/// \endif
	/// </param>
	private void OwnerWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			SetWindowOwner(null);
			Hide();
		}
	}

	/// <summary>
	/// \if KO
	/// <para>가상 키보드 창의 WPF 소유 창을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets the WPF owner of the virtual-keyboard window.</para>
	/// \endif
	/// </summary>
	/// <param name="owner">
	/// \if KO
	/// <para>새 소유 창이며 관계 제거 시 <see langword="null"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The new owner, or <see langword="null"/> to clear ownership.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// \if KO
	/// <para>잘못된 소유 관계를 설정할 때 WPF에서 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown by WPF for an invalid ownership relationship.</para>
	/// \endif
	/// </exception>
	private void SetWindowOwner(Window? owner)
	{
		Owner = owner;
	}

	/// <summary>
	/// \if KO
	/// <para>암호 입력은 대상의 Password 속성에 쓰고 일반 입력은 텍스트 바인딩 소스를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Writes password input to a target Password property or updates the text binding source for ordinary input.</para>
	/// \endif
	/// </summary>
	private void UpdateSourceBinding()
	{

		if (VirtualKeyboardComp.VkbPasswordBox.Password != string.Empty)
		{
			if (TryFindPasswordLikeElement(_placementTarget!, out var sink))
			{
				if (sink is PasswordBox childPwd)
				{
					childPwd.Password = VirtualKeyboardComp.VkbPasswordBox.Password;
					return;
				}

				if (TryWritePassword(sink, VirtualKeyboardComp.VkbPasswordBox.Password))
					return;
			}
		}
		else
		{
			VirtualKeyboardComp.UpdateSourceBinding();
		}
	}

	/// <summary>
	/// \if KO
	/// <para>루트 또는 시각적 후손에서 쓰기 가능한 문자열 Password 속성을 가진 요소를 찾습니다.</para>
	/// \endif
	/// \if EN
	/// <para>Finds an element with a writable string Password property at the root or among visual descendants.</para>
	/// \endif
	/// </summary>
	/// <param name="root">
	/// \if KO
	/// <para>탐색 시작 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The search root.</para>
	/// \endif
	/// </param>
	/// <param name="element">
	/// \if KO
	/// <para>성공 시 찾은 요소입니다.</para>
	/// \endif
	/// \if EN
	/// <para>On success, the located element.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>암호 입력 요소를 찾았으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if a password-like element was found.</para>
	/// \endif
	/// </returns>
	private static bool TryFindPasswordLikeElement(DependencyObject root, out FrameworkElement element)
	{
		element = null!;

		// A) 표준 PasswordBox 우선
		if (root.FindFirstVisualChild<PasswordBox>() is { } pwdBox)
		{
			element = pwdBox;
			return true;
		}

		// B) 루트 자체가 Password CLR 속성 보유면 사용
		if (root is FrameworkElement feRoot && HasPasswordProperty(root))
		{
			element = feRoot;
			return true;
		}

		// C) 하위에서 Password CLR 속성 보유 요소 탐색
		if (FindDescendantWithPasswordProperty(root) is FrameworkElement fe)
		{
			element = fe;
			return true;
		}

		return false;
	}

	/// <summary>
	/// \if KO
	/// <para>대상 형식에 읽고 쓸 수 있는 문자열 <c>Password</c> CLR 속성이 있는지 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether a target type has a readable and writable string <c>Password</c> CLR property.</para>
	/// \endif
	/// </summary>
	/// <param name="target">
	/// \if KO
	/// <para>검사할 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object to inspect.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>호환 속성이 있으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if a compatible property exists.</para>
	/// \endif
	/// </returns>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="target"/>이 <see langword="null"/>일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="target"/> is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	private static bool HasPasswordProperty(DependencyObject target)
	{
		var t = target.GetType();
		var p = t.GetProperty(
			"Password",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
		);
		return p != null && p.CanRead && p.CanWrite && p.PropertyType == typeof(string);
	}

	/// <summary>
	/// \if KO
	/// <para>리플렉션으로 대상의 쓰기 가능한 문자열 Password 속성에 값을 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Writes a value to the target's writable string Password property through reflection.</para>
	/// \endif
	/// </summary>
	/// <param name="target">
	/// \if KO
	/// <para>암호 값을 받을 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object receiving the password.</para>
	/// \endif
	/// </param>
	/// <param name="value">
	/// \if KO
	/// <para>설정할 암호 문자열입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The password string to set.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>호환 속성을 찾아 설정했으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if a compatible property was found and set.</para>
	/// \endif
	/// </returns>
	/// <exception cref="System.Reflection.TargetInvocationException">
	/// \if KO
	/// <para>Password 속성 setter가 예외를 발생시키면 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when the Password setter throws.</para>
	/// \endif
	/// </exception>
	private static bool TryWritePassword(DependencyObject target, string value)
	{
		var t = target.GetType();
		var p = t.GetProperty(
			"Password",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
		);

		if (p != null && p.CanWrite && p.PropertyType == typeof(string))
		{
			p.SetValue(target, value);
			return true;
		}
		return false;
	}

	/// <summary>
	/// \if KO
	/// <para>시각적 후손에서 쓰기 가능한 문자열 Password 속성을 가진 첫 요소를 찾습니다.</para>
	/// \endif
	/// \if EN
	/// <para>Finds the first visual descendant with a writable string Password property.</para>
	/// \endif
	/// </summary>
	/// <param name="root">
	/// \if KO
	/// <para>탐색 시작 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The search root.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>첫 일치 요소이며 없으면 <see langword="null"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The first matching element, or <see langword="null"/> if absent.</para>
	/// \endif
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// \if KO
	/// <para>루트가 유효한 시각적 객체가 아닐 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when the root is not a valid visual object.</para>
	/// \endif
	/// </exception>
	private static FrameworkElement? FindDescendantWithPasswordProperty(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);

			if (child is FrameworkElement fe && HasPasswordProperty(child))
				return fe;

			var deeper = FindDescendantWithPasswordProperty(child);
			if (deeper != null)
				return deeper;
		}
		return null;
	}

	/// <summary>
	/// \if KO
	/// <para>키보드 UI의 숫자 제한 레이블 표시 상태를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Updates numeric-limit label visibility in the keyboard UI.</para>
	/// \endif
	/// </summary>
	private void UpdateVisibilityState()
	{
		VirtualKeyboardComp.UpdateNumericLabelVisibility();
	}

	/// <summary>
	/// \if KO
	/// <para>암호 입력과 검증 메시지 텍스트를 비웁니다.</para>
	/// \endif
	/// \if EN
	/// <para>Clears password input and validation message text.</para>
	/// \endif
	/// </summary>
	private void ClearText()
	{
		VirtualKeyboardComp.VkbPasswordBox.Password = string.Empty;
		VirtualKeyboardComp.MsgTbl.Text = string.Empty;
	}

	/// <summary>
	/// \if KO
	/// <para>배치 대상의 텍스트 입력 또는 대상 자체로 포커스를 복원합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Restores focus to a text input under the placement target or to the target itself.</para>
	/// \endif
	/// </summary>
	private void FocusPlacementTarget()
	{
		if (_placementTarget?.FindFirstVisualChild<System.Windows.Controls.TextBox>() is { } textBox)
		{
			textBox.Focus();
			textBox.CaretIndex = textBox.Text.Length;
		}
		else if (_placementTarget is UIElement passwordBox)
		{
			passwordBox.Focus();
		}
	}

	/// <summary>
	/// \if KO
	/// <para>모니터 작업 영역 조회에 필요한 Win32 구조체와 함수를 제공합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Provides Win32 structures and functions required to query monitor work areas.</para>
	/// \endif
	/// </summary>
	private static class NativeMethods
	{
		/// <summary>
		/// \if KO
		/// <para>지정 지점에서 가장 가까운 모니터를 선택하는 플래그입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Specifies selection of the monitor nearest a point.</para>
		/// \endif
		/// </summary>
		public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

		/// <summary>
		/// \if KO
		/// <para>Win32 정수 화면 좌표를 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Represents Win32 integer screen coordinates.</para>
		/// \endif
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			/// <summary>
			/// \if KO
			/// <para>수평 좌표입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The horizontal coordinate.</para>
			/// \endif
			/// </summary>
			public int x;
			/// <summary>
			/// \if KO
			/// <para>수직 좌표입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The vertical coordinate.</para>
			/// \endif
			/// </summary>
			public int y;
		}

		/// <summary>
		/// \if KO
		/// <para>Win32 사각형 경계를 나타냅니다.</para>
		/// \endif
		/// \if EN
		/// <para>Represents Win32 rectangle bounds.</para>
		/// \endif
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			/// <summary>
			/// \if KO
			/// <para>왼쪽 경계입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The left bound.</para>
			/// \endif
			/// </summary>
			public int left;
			/// <summary>
			/// \if KO
			/// <para>위쪽 경계입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The top bound.</para>
			/// \endif
			/// </summary>
			public int top;
			/// <summary>
			/// \if KO
			/// <para>오른쪽 경계입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The right bound.</para>
			/// \endif
			/// </summary>
			public int right;
			/// <summary>
			/// \if KO
			/// <para>아래쪽 경계입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The bottom bound.</para>
			/// \endif
			/// </summary>
			public int bottom;
		}

		/// <summary>
		/// \if KO
		/// <para>모니터 전체 및 작업 영역 정보를 받는 Win32 구조체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Represents the Win32 structure receiving full-monitor and work-area information.</para>
		/// \endif
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MONITORINFO
		{
			/// <summary>
			/// \if KO
			/// <para>구조체 바이트 크기입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The structure size in bytes.</para>
			/// \endif
			/// </summary>
			public uint cbSize;
			/// <summary>
			/// \if KO
			/// <para>모니터 전체 영역입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The full monitor bounds.</para>
			/// \endif
			/// </summary>
			public RECT rcMonitor;
			/// <summary>
			/// \if KO
			/// <para>작업 표시줄을 제외한 작업 영역입니다.</para>
			/// \endif
			/// \if EN
			/// <para>The work area excluding taskbars.</para>
			/// \endif
			/// </summary>
			public RECT rcWork;
			/// <summary>
			/// \if KO
			/// <para>모니터 특성 플래그입니다.</para>
			/// \endif
			/// \if EN
			/// <para>Monitor attribute flags.</para>
			/// \endif
			/// </summary>
			public uint dwFlags;
		}

		/// <summary>
		/// \if KO
		/// <para>화면 지점과 연결된 모니터 핸들을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the monitor handle associated with a screen point.</para>
		/// \endif
		/// </summary>
		/// <param name="pt">
		/// \if KO
		/// <para>화면 좌표입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The screen point.</para>
		/// \endif
		/// </param>
		/// <param name="dwFlags">
		/// \if KO
		/// <para>모니터 선택 플래그입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Monitor-selection flags.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>모니터 핸들입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The monitor handle.</para>
		/// \endif
		/// </returns>
		[DllImport("user32.dll")]
		public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

		/// <summary>
		/// \if KO
		/// <para>지정한 모니터의 영역 정보를 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets area information for a monitor.</para>
		/// \endif
		/// </summary>
		/// <param name="hMonitor">
		/// \if KO
		/// <para>모니터 핸들입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The monitor handle.</para>
		/// \endif
		/// </param>
		/// <param name="lpmi">
		/// \if KO
		/// <para>정보를 받을 구조체입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The structure receiving information.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>성공 여부입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Whether the operation succeeded.</para>
		/// \endif
		/// </returns>
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
	}
}

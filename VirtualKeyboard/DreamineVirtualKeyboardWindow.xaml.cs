using System.ComponentModel;
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
/// Interaction logic for DreamineVirtualKeyboardWindow.xaml
/// </summary>
public partial class DreamineVirtualKeyboardWindow : Window
{
	private DependencyObject? _placementTarget;

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

	private void DreamineVirtualKeyboardWindow_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		CalcKeyboardPosition();
	}

	private void DreamineVirtualKeyboardWindow_LocationChanged(object? sender, EventArgs e)
	{
		VirtualKeyboardComp.FocusVkbTextBox();
	}

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

	private void DreamineVirtualKeyboardWindow_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
			this.DragMove();
	}

	private async void DreamineVirtualKeyboardWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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
				Hide();
				break;
			case System.Windows.Input.Key.Escape:
				FocusPlacementTarget();
				ClearText();
				await Task.Delay(100);
				Hide();
				break;
			default:
				break;
		}
	}

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

	private void DreamineVirtualKeyboardWindow_Closing(object? sender, CancelEventArgs e)
	{
		DreamineVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);
		VirtualKeyboardComp?.Dispose();
		DreamineVirtualKeyboardAssist.ResetDreamineVirtualKeyboard();
	}

	private void Application_Exit(object sender, ExitEventArgs e)
	{
		VirtualKeyboardComp?.Dispose();
	}


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


	public void SetLayout(VkLayout layout)
	{
		VirtualKeyboardComp.Layout = layout;

		SetDisplayKeyboardSize(layout);

		SetDefaultMinMaxValue(layout);

		UpdateVisibilityState();
	}

	private void SetDisplayKeyboardSize(VkLayout layout)
	{
		MaxWidth = layout == VkLayout.Text || layout == VkLayout.Password ? 948 : 548;
	}

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

	private void OwnerWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			SetWindowOwner(null);
			Hide();
		}
	}

	private void SetWindowOwner(Window? owner)
	{
		Owner = owner;
	}

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

	private static bool HasPasswordProperty(DependencyObject target)
	{
		var t = target.GetType();
		var p = t.GetProperty(
			"Password",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
		);
		return p != null && p.CanRead && p.CanWrite && p.PropertyType == typeof(string);
	}

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

	private void UpdateVisibilityState()
	{
		VirtualKeyboardComp.UpdateNumericLabelVisibility();
	}

	private void ClearText()
	{
		VirtualKeyboardComp.VkbPasswordBox.Password = string.Empty;
		VirtualKeyboardComp.MsgTbl.Text = string.Empty;
	}

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

	private static class NativeMethods
	{
		public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT { public int x; public int y; }

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT { public int left; public int top; public int right; public int bottom; }

		[StructLayout(LayoutKind.Sequential)]
		public struct MONITORINFO
		{
			public uint cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
		}

		[DllImport("user32.dll")]
		public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
	}
}

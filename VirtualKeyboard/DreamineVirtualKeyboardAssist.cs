using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public static class DreamineVirtualKeyboardAssist
{
    #region Variable

    private static DreamineVirtualKeyboardWindow _virtualKeyboardWindow = new();
    private static DependencyObject? _placementTarget = null;

    #endregion

    #region Global Property

    public static bool EnableDreamineVirtualKeyboard { get; set; } = true;

    #endregion

    #region Func/Action

    public static Func<DependencyObject, VkLayout>? GetKeyboardLayoutAction { get; set; } = null;
    public static Action<DependencyObject, VkLayout, DreamineVirtualKeyboardWindow>? SetBindingAction { get; set; } = null;

    #endregion

    #region Dependency Property

    #region Layout Property

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.RegisterAttached(
            "Layout",
            typeof(VkLayout),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(default(VkLayout)));

    public static VkLayout GetLayout(DependencyObject obj) =>
        (VkLayout)obj.GetValue(LayoutProperty);

    public static void SetLayout(DependencyObject obj, VkLayout value) =>
        obj.SetValue(LayoutProperty, value);

    #endregion

    #region UseVirtualKeyBoard Property

    public static readonly DependencyProperty UseVirtualKeyBoardProperty =
    DependencyProperty.RegisterAttached(
        "UseVirtualKeyBoard",
        typeof(bool),
        typeof(DreamineVirtualKeyboardAssist),
        new PropertyMetadata(false, OnUseVirtualKeyBoardChanged));

    public static void SetUseVirtualKeyBoard(DependencyObject obj, bool value) =>
        obj.SetValue(UseVirtualKeyBoardProperty, value);

    public static bool GetUseVirtualKeyBoard(DependencyObject obj) =>
        (bool)obj.GetValue(UseVirtualKeyBoardProperty);

    #endregion

    #region Minimum Property

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.RegisterAttached(
            "Minimum",
            typeof(decimal),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MinValue));

    public static void SetMinimum(DependencyObject obj, decimal value) =>
        obj.SetValue(MinimumProperty, value);

    public static decimal GetMinimum(DependencyObject obj) =>
        (decimal)obj.GetValue(MinimumProperty);

    #endregion

    #region Maximum Property

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.RegisterAttached(
            "Maximum",
            typeof(decimal),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MaxValue));

    public static void SetMaximum(DependencyObject obj, decimal value) =>
        obj.SetValue(MaximumProperty, value);

    public static decimal GetMaximum(DependencyObject obj) =>
        (decimal)obj.GetValue(MaximumProperty);

    #endregion

    #region DecimalFormat Property

    public static readonly DependencyProperty DecimalFormatProperty =
        DependencyProperty.RegisterAttached(
            "DecimalFormat",
            typeof(string),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(string.Empty));

    public static void SetDecimalFormat(DependencyObject obj, string value) =>
        obj.SetValue(DecimalFormatProperty, value);

    public static string GetDecimalFormat(DependencyObject obj) =>
        (string)obj.GetValue(DecimalFormatProperty);

    #endregion

    #region DreamineVkbIconAdorner Property

    private static readonly DependencyProperty DreamineVkbIconAdornerProperty =
       DependencyProperty.RegisterAttached(
           "DreamineVkbIconAdorner", typeof(DreamineVkbIconAdorner), typeof(DreamineVirtualKeyboardAssist),
           new PropertyMetadata(null));
    private static void SetDreamineVkbIconAdorner(UIElement element, DreamineVkbIconAdorner value)
        => element.SetValue(DreamineVkbIconAdornerProperty, value);
    private static DreamineVkbIconAdorner? GetDreamineVkbIconAdorner(UIElement element)
        => (DreamineVkbIconAdorner?)element.GetValue(DreamineVkbIconAdornerProperty);

	/// <summary>
	/// @brief 가상 키보드 아이콘(Adorner) 표시 여부.
	/// @details
	/// - Style/Trigger에서 ON/OFF 하십시오.
	/// - True가 되면 대상 요소 위에 <see cref="DreamineVkbIconAdorner"/> 를 부착합니다.
	/// </details>
	public static readonly DependencyProperty VkbIconVisibleProperty =
		DependencyProperty.RegisterAttached(
			"VkbIconVisible",
			typeof(bool),
			typeof(DreamineVirtualKeyboardAssist),
			new PropertyMetadata(false, OnVkbIconVisibleChanged));

	/// <summary> @brief VkbIconVisible getter </summary>
	public static bool GetVkbIconVisible(DependencyObject obj)
		=> (bool)obj.GetValue(VkbIconVisibleProperty);

	/// <summary> @brief VkbIconVisible setter </summary>
	public static void SetVkbIconVisible(DependencyObject obj, bool value)
		=> obj.SetValue(VkbIconVisibleProperty, value);

	/// <summary>
	/// @brief VkbIconVisible 변경 시 Adorner를 추가/제거합니다.
	/// @details
	/// - <see cref="FrameworkElement"/> 이면 Loaded 시점까지 대기.
	/// - 순수 <see cref="UIElement"/> 는 Dispatcher로 지연 적용.
	/// </details>
	private static void OnVkbIconVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement el) return;

		void Apply()
		{
			var layer = AdornerLayer.GetAdornerLayer(el);
			if (layer == null) return;

			bool visible = (bool)e.NewValue;
			var current = GetDreamineVkbIconAdorner(el);

			if (visible)
			{
				if (current == null)
				{
					var vkbIcon = new DreamineVkbIconAdorner(el);
					vkbIcon.SetPreviewMouseDownAction((_, _) => _virtualKeyboardWindow?.Hide());
					layer.Add(vkbIcon);
					SetDreamineVkbIconAdorner(el, vkbIcon);
				}
			}
			else
			{
				if (current != null)
				{
					layer.Remove(current);
					SetDreamineVkbIconAdorner(el, null!);
				}
			}
		}

		if (d is FrameworkElement fe)
		{
			if (fe.IsLoaded)
			{
				Apply();
			}
			else
			{
				RoutedEventHandler? h = null;
				h = (_, __) =>
				{
					fe.Loaded -= h;
					Apply();
				};
				fe.Loaded += h;
			}
		}
		else
		{
			// UIElement만 있는 경우: Loaded 없음 → Dispatcher로 지연
			el.Dispatcher.InvokeAsync(Apply, DispatcherPriority.Loaded);
		}
	}

	#endregion

	#endregion

	#region Action Provider Property

	public static readonly DependencyProperty EnterActionProviderProperty =
        DependencyProperty.RegisterAttached(
            "EnterActionProvider",
            typeof(IEnterActionProvider),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(null, OnEnterActionProviderChanged));

    public static IEnterActionProvider? GetEnterActionProvider(DependencyObject obj)
        => (IEnterActionProvider?)obj.GetValue(EnterActionProviderProperty);

    public static void SetEnterActionProvider(DependencyObject obj, IEnterActionProvider value)
        => obj.SetValue(EnterActionProviderProperty, value);

    private static void OnEnterActionProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is IEnterActionProvider provider)
        {
            provider.PlacementTarget = d;
        }
    }
    #endregion

    #region

    static DreamineVirtualKeyboardAssist()
    {
        SetBindingAction = SetBinding;
        GetKeyboardLayoutAction = GetLayout;
    }

    #endregion


    #region Event Handlers

    private static void OnUseVirtualKeyBoardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(e.NewValue is bool enabled) || d is not UIElement ui) return;

        if (enabled)
        {
            ui.PreviewMouseDown += OnControlMouseDown;
            ui.IsEnabledChanged += OnIsEnabledChanged;
            ui.IsHitTestVisibleChanged += OnIsEnabledChanged;
            ui.IsVisibleChanged += OnIsVisibleChanged;
        }
        else
        {
            ui.PreviewMouseDown -= OnControlMouseDown;
            ui.IsEnabledChanged -= OnIsEnabledChanged;
            ui.IsHitTestVisibleChanged -= OnIsEnabledChanged;
            ui.IsVisibleChanged -= OnIsVisibleChanged;
        }
    }

    private static void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isEnable = (bool)e.NewValue;

        if (!isEnable && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    private static void OnControlMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            ToggleVirtualKeyBoard(d);
        }
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isVisible = (bool)e.NewValue;
        if (!isVisible && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    private static void ToggleVirtualKeyBoard(DependencyObject placementTarget)
    {
        if (!EnableDreamineVirtualKeyboard)
        {
            return;
        }

        if ( _virtualKeyboardWindow.IsVisible && !TargetChanged(placementTarget) || IsReadOnly(placementTarget) == true)
        {
            _virtualKeyboardWindow.Hide();
            return;
        }

        ShowDreamineVirtualKeyboard(placementTarget);
    }

    #endregion

    #region Public Methods

    public static bool TargetChanged(DependencyObject? placementTarget)
    {
        return _placementTarget != placementTarget;
    }

    public static void ShowDreamineVirtualKeyboard(DependencyObject? placementTarget)
    {
        if (placementTarget is null)
        {
            return;
        }

        SetVkbIconVisibility(placementTarget, true);

        _placementTarget = placementTarget;
        var layout = GetKeyboardLayoutAction?.Invoke(placementTarget) ?? default;

        _virtualKeyboardWindow.SetLayout(layout);
        _virtualKeyboardWindow.SetPlacementTarget(_placementTarget);

        SetBindingAction?.Invoke(placementTarget, layout, _virtualKeyboardWindow);

        _virtualKeyboardWindow.Show();
        _virtualKeyboardWindow.Activate();
    }

    public static void SetVkbIconVisibility(DependencyObject? placementTarget, bool visible)
    {
        if (placementTarget is not UIElement uIElement || AdornerLayer.GetAdornerLayer(uIElement) is not { } adornerLayer)
        {
            return;
        }

        if (visible)
        {
            var vkbIcon = new DreamineVkbIconAdorner(uIElement);
            vkbIcon.SetPreviewMouseDownAction((_, _) =>
            {
                _virtualKeyboardWindow?.Hide();
            });
            adornerLayer.Add(vkbIcon);
            SetDreamineVkbIconAdorner(uIElement, vkbIcon);
        }
        else
        {
            if (GetDreamineVkbIconAdorner(uIElement) is { } vkbIcon)
            {
                adornerLayer.Remove(vkbIcon);
            }
        }
    }
    	
	public static void SetBinding(DependencyObject placementTarget, VkLayout layout, DreamineVirtualKeyboardWindow virtualKeyboardWindow)
	{
		if (layout == VkLayout.Password)
		{
			if (TryBindPasswordProperty(placementTarget, out var pwdBinding))
			{
				virtualKeyboardWindow.SetBinding(pwdBinding);
				return;
			}

			if (placementTarget is PasswordBox)
			{
				virtualKeyboardWindow.SetBinding(null!);
				return;
			}

			virtualKeyboardWindow.SetBinding(null!);
			return;
		}

		if (placementTarget is TextBox textBox)
		{
			var binding = new Binding("Text")
			{
				Source = textBox,
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			var min = GetMinimum(placementTarget);
			var max = GetMaximum(placementTarget);

			switch (layout)
			{
				case VkLayout.Numeric:
					virtualKeyboardWindow.SetBinding(binding, Math.Max(int.MinValue, (int)min), Math.Min(int.MaxValue, (int)max));
					return;

				case VkLayout.Decimal:
					var decimalFormat = GetDecimalFormat(placementTarget);
					virtualKeyboardWindow.SetBinding(binding, min, max, decimalFormat);
					return;

				default: // Text
					virtualKeyboardWindow.SetBinding(binding);
					return;
			}
		}

		if (HasPasswordProperty(placementTarget))
		{
			virtualKeyboardWindow.SetBinding(null!);
			return;
		}

		if (placementTarget is PasswordBox)
		{
			virtualKeyboardWindow.SetBinding(null!);
			return;
		}

		virtualKeyboardWindow.SetBinding(null!);
	}

	private static bool TryBindPasswordProperty(DependencyObject target, out Binding binding)
	{
		binding = null!;

		var type = target.GetType();

		var clrProp = type.GetProperty("Password");
		if (clrProp != null &&
			clrProp.CanRead &&
			clrProp.CanWrite &&
			clrProp.PropertyType == typeof(string))
		{
			binding = new Binding("Password")
			{
				Source = target,
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};
			return true;
		}

		var dpField = type.GetField("PasswordProperty",
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.Static |
			System.Reflection.BindingFlags.FlattenHierarchy);

		if (dpField?.GetValue(null) is DependencyProperty)
		{
			binding = new Binding("Password")
			{
				Source = target,
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};
			return true;
		}

		return false;
	}

	private static bool HasPasswordProperty(DependencyObject target)
	{
		var type = target.GetType();
		if (type.GetProperty("Password") is { } p &&
			p.CanRead && p.CanWrite && p.PropertyType == typeof(string))
			return true;

		var dpField = type.GetField("PasswordProperty",
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.Static |
			System.Reflection.BindingFlags.FlattenHierarchy);
		return dpField?.GetValue(null) is DependencyProperty;
	}

	public static void ResetDreamineVirtualKeyboard()
    {
        _virtualKeyboardWindow = null!;
        _placementTarget = null;
    }

	/// <summary>
	/// 앱 종료 시 SharpHook 스레드를 포함한 모든 리소스를 해제합니다.
	/// Application.OnExit 또는 MainWindow.Closed 에서 호출하세요.
	/// </summary>
	/// <summary>
	/// 앱 종료 시 SharpHook 스레드를 포함한 모든 리소스를 해제합니다.
	/// Application.OnExit 또는 MainWindow.Closed 에서 호출하세요.
	/// </summary>
	public static void Shutdown()
	{
		if (_virtualKeyboardWindow is { } win)
		{
			try
			{
				// 비주얼 트리에서 DreamineVirtualKeyboard를 찾아 Dispose → SharpHook 중지
				DisposeVkControl(win);
			}
			catch { }

			try { if (win.IsLoaded) win.Close(); } catch { }
		}

		_virtualKeyboardWindow = null!;
		_placementTarget = null;
	}

	private static void DisposeVkControl(System.Windows.DependencyObject root)
	{
		if (root is DreamineVirtualKeyboard vk) { vk.Dispose(); return; }
		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
			DisposeVkControl(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
	}

    public static List<IEnterActionProvider> GetEnterActionProviders(DependencyObject d)
    {
        var list = new List<IEnterActionProvider>();

        if (GetEnterActionProvider(d) is { } provider)
        {
            list.Add(provider);
        }

        if (TreeHelper.FindParent<DreamineEnterActionGroupProvider>(d) is { } groupControl)
        {
            var subProviders = GetProviders(groupControl, []) ?? [];
            list.AddRange(subProviders.Where(x => x.PlacementTarget != d));

            if (groupControl.Commit is { } commitProvider)
            {
                if (subProviders.Any())
                {
                    commitProvider.PlacementTarget = subProviders.Last().PlacementTarget;
                }

                list.Add(commitProvider);
            }
        }

        return list;
    }

    private static IEnumerable<IEnterActionProvider> GetProviders(DependencyObject root, DependencyObject[] excepts)
    {
        if (root == null) yield break;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (!excepts.Contains(child) && GetEnterActionProvider(child) is { } provider)
            {
                yield return provider;
            }

            foreach (var descendant in GetProviders(child, excepts))
                yield return descendant;
        }
    }

    private static bool? IsReadOnly(DependencyObject placementTarget)
    {
        var prop = placementTarget.GetType().GetProperty("IsReadOnly");
        return (bool?)prop?.GetValue(placementTarget);
    }

    #endregion
}

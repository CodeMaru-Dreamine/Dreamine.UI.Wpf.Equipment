using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>WPF 입력 요소에 가상 키보드 설정·표시·바인딩·Enter 동작을 연결합니다.</para>
/// \endif
/// \if EN
/// <para>Attaches virtual-keyboard configuration, display, binding, and Enter actions to WPF input elements.</para>
/// \endif
/// </summary>
public static class DreamineVirtualKeyboardAssist
{
    #region Variable

    /// <summary>
    /// \if KO
    /// <para>모든 연결 입력 요소가 공유하는 가상 키보드 창입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the virtual-keyboard window shared by all attached input elements.</para>
    /// \endif
    /// </summary>
    private static DreamineVirtualKeyboardWindow _virtualKeyboardWindow = new();
    /// <summary>
    /// \if KO
    /// <para>현재 키보드가 편집하는 배치 대상을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the placement target currently edited by the keyboard.</para>
    /// \endif
    /// </summary>
    private static DependencyObject? _placementTarget = null;

    #endregion

    #region Global Property

    /// <summary>
    /// \if KO
    /// <para>애플리케이션 전체 가상 키보드 표시를 허용할지 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether virtual-keyboard display is globally enabled.</para>
    /// \endif
    /// </summary>
    public static bool EnableDreamineVirtualKeyboard { get; set; } = true;

    #endregion

    #region Func/Action

    /// <summary>
    /// \if KO
    /// <para>배치 대상에서 키보드 레이아웃을 확인하는 확장 함수를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the extension function that resolves keyboard layout from a target.</para>
    /// \endif
    /// </summary>
    public static Func<DependencyObject, VkLayout>? GetKeyboardLayoutAction { get; set; } = null;
    /// <summary>
    /// \if KO
    /// <para>대상·레이아웃·창 사이의 값 바인딩을 구성하는 확장 동작을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the extension action that configures value binding among target, layout, and window.</para>
    /// \endif
    /// </summary>
    public static Action<DependencyObject, VkLayout, DreamineVirtualKeyboardWindow>? SetBindingAction { get; set; } = null;

    #endregion

    #region Dependency Property

    #region Layout Property

    /// <summary>
    /// \if KO
    /// <para>입력 요소의 가상 키보드 레이아웃 연결 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the attached virtual-keyboard layout property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.RegisterAttached(
            "Layout",
            typeof(VkLayout),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(default(VkLayout)));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 가상 키보드 레이아웃을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the virtual-keyboard layout from an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>값을 읽을 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object from which to read the value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>구성된 레이아웃입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The configured layout.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static VkLayout GetLayout(DependencyObject obj) =>
        (VkLayout)obj.GetValue(LayoutProperty);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 가상 키보드 레이아웃을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the virtual-keyboard layout on an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>값을 설정할 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object on which to set the value.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>설정할 레이아웃입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The layout to set.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetLayout(DependencyObject obj, VkLayout value) =>
        obj.SetValue(LayoutProperty, value);

    #endregion

    #region UseVirtualKeyBoard Property

    /// <summary>
    /// \if KO
    /// <para>입력 요소의 가상 키보드 사용 여부 연결 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the attached virtual-keyboard usage property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty UseVirtualKeyBoardProperty =
    DependencyProperty.RegisterAttached(
        "UseVirtualKeyBoard",
        typeof(bool),
        typeof(DreamineVirtualKeyboardAssist),
        new PropertyMetadata(false, OnUseVirtualKeyBoardChanged));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 가상 키보드 사용 여부를 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets whether an object uses the virtual keyboard.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>설정 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>사용할지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Whether to enable usage.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetUseVirtualKeyBoard(DependencyObject obj, bool value) =>
        obj.SetValue(UseVirtualKeyBoardProperty, value);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체가 가상 키보드를 사용하는지 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether an object uses the virtual keyboard.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>조회 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The usage state.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static bool GetUseVirtualKeyBoard(DependencyObject obj) =>
        (bool)obj.GetValue(UseVirtualKeyBoardProperty);

    #endregion

    #region Minimum Property

    /// <summary>
    /// \if KO
    /// <para>숫자 입력 최소값 연결 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the attached minimum numeric value property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.RegisterAttached(
            "Minimum",
            typeof(decimal),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MinValue));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 숫자 입력 최소값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the minimum numeric input value on an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>설정 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>최소값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The minimum value.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetMinimum(DependencyObject obj, decimal value) =>
        obj.SetValue(MinimumProperty, value);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 숫자 입력 최소값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the minimum numeric input value from an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>조회 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>구성된 최소값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The configured minimum.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static decimal GetMinimum(DependencyObject obj) =>
        (decimal)obj.GetValue(MinimumProperty);

    #endregion

    #region Maximum Property

    /// <summary>
    /// \if KO
    /// <para>숫자 입력 최대값 연결 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the attached maximum numeric value property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.RegisterAttached(
            "Maximum",
            typeof(decimal),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MaxValue));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 숫자 입력 최대값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the maximum numeric input value on an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>설정 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>최대값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The maximum value.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetMaximum(DependencyObject obj, decimal value) =>
        obj.SetValue(MaximumProperty, value);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 숫자 입력 최대값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the maximum numeric input value from an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>조회 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>구성된 최대값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The configured maximum.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static decimal GetMaximum(DependencyObject obj) =>
        (decimal)obj.GetValue(MaximumProperty);

    #endregion

    #region DecimalFormat Property

    /// <summary>
    /// \if KO
    /// <para>소수 표시 형식 연결 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the attached decimal display-format property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty DecimalFormatProperty =
        DependencyProperty.RegisterAttached(
            "DecimalFormat",
            typeof(string),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 소수 표시 형식을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the decimal display format on an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>설정 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>.NET 숫자 형식 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The .NET numeric format string.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetDecimalFormat(DependencyObject obj, string value) =>
        obj.SetValue(DecimalFormatProperty, value);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 소수 표시 형식을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the decimal display format from an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>조회 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>구성된 형식 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The configured format string.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static string GetDecimalFormat(DependencyObject obj) =>
        (string)obj.GetValue(DecimalFormatProperty);

    #endregion

    #region DreamineVkbIconAdorner Property

    /// <summary>
    /// \if KO
    /// <para>대상에 부착된 키보드 아이콘 장식자를 보관하는 내부 연결 속성입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the internal attached property storing the keyboard icon adorner.</para>
    /// \endif
    /// </summary>
    private static readonly DependencyProperty DreamineVkbIconAdornerProperty =
       DependencyProperty.RegisterAttached(
           "DreamineVkbIconAdorner", typeof(DreamineVkbIconAdorner), typeof(DreamineVirtualKeyboardAssist),
           new PropertyMetadata(null));
    /// <summary>
    /// \if KO
    /// <para>대상 요소에 아이콘 장식자 참조를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores an icon-adorner reference on a target element.</para>
    /// \endif
    /// </summary>
    /// <param name="element">
    /// \if KO
    /// <para>대상 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target element.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>저장할 장식자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The adorner to store.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="element"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="element"/> is null.</para>
    /// \endif
    /// </exception>
    private static void SetDreamineVkbIconAdorner(UIElement element, DreamineVkbIconAdorner value)
        => element.SetValue(DreamineVkbIconAdornerProperty, value);
    /// <summary>
    /// \if KO
    /// <para>대상 요소에 저장된 아이콘 장식자를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the icon adorner stored on a target element.</para>
    /// \endif
    /// </summary>
    /// <param name="element">
    /// \if KO
    /// <para>대상 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target element.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>저장된 장식자이며 없으면 null입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The stored adorner, or null if absent.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="element"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="element"/> is null.</para>
    /// \endif
    /// </exception>
    private static DreamineVkbIconAdorner? GetDreamineVkbIconAdorner(UIElement element)
        => (DreamineVkbIconAdorner?)element.GetValue(DreamineVkbIconAdornerProperty);

	/// <summary>
	/// \if KO
	/// <para>대상 요소 위의 가상 키보드 아이콘 장식자 표시 여부 연결 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the attached property controlling virtual-keyboard icon adorner visibility.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty VkbIconVisibleProperty =
		DependencyProperty.RegisterAttached(
			"VkbIconVisible",
			typeof(bool),
			typeof(DreamineVirtualKeyboardAssist),
			new PropertyMetadata(false, OnVkbIconVisibleChanged));

	/// <summary>
	/// \if KO
	/// <para>지정한 객체의 키보드 아이콘 표시 여부를 가져옵니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets keyboard-icon visibility from an object.</para>
	/// \endif
	/// </summary>
	/// <param name="obj">
	/// \if KO
	/// <para>조회 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The target object.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>표시 여부입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The visibility state.</para>
	/// \endif
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// \if KO
	/// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="obj"/> is null.</para>
	/// \endif
	/// </exception>
	public static bool GetVkbIconVisible(DependencyObject obj)
		=> (bool)obj.GetValue(VkbIconVisibleProperty);

	/// <summary>
	/// \if KO
	/// <para>지정한 객체의 키보드 아이콘 표시 여부를 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Sets keyboard-icon visibility on an object.</para>
	/// \endif
	/// </summary>
	/// <param name="obj">
	/// \if KO
	/// <para>설정 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The target object.</para>
	/// \endif
	/// </param>
	/// <param name="value">
	/// \if KO
	/// <para>표시 여부입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The visibility state.</para>
	/// \endif
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// \if KO
	/// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="obj"/> is null.</para>
	/// \endif
	/// </exception>
	public static void SetVkbIconVisible(DependencyObject obj, bool value)
		=> obj.SetValue(VkbIconVisibleProperty, value);

	/// <summary>
	/// \if KO
	/// <para>아이콘 표시 설정 변경 시 로드 시점 또는 디스패처에서 장식자를 추가하거나 제거합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Adds or removes the icon adorner on load or through the dispatcher when visibility changes.</para>
	/// \endif
	/// </summary>
	/// <param name="d">
	/// \if KO
	/// <para>설정이 변경된 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The object whose setting changed.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>새 표시 상태를 포함하는 변경 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Change data containing the new visibility state.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidCastException">
	/// \if KO
	/// <para>새 값이 bool이 아닐 때 지연 처리 중 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown during deferred processing when the new value is not a Boolean.</para>
	/// \endif
	/// </exception>
	private static void OnVkbIconVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement el) return;

		#pragma warning disable CS1587
		/// \cond LOCAL_FUNCTION_DOCUMENTATION
		/// <summary>
		/// \if KO
		/// <para>가상 키보드 아이콘 Adorner의 표시 상태를 현재 요소에 적용합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Applies the virtual-keyboard icon adorner visibility to the current element.</para>
		/// \endif
		/// </summary>
		/// \endcond
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
		#pragma warning restore CS1587

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

	/// <summary>
	/// \if KO
	/// <para>Enter 키 확정 동작 공급자 연결 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the attached Enter-key commit action-provider property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty EnterActionProviderProperty =
        DependencyProperty.RegisterAttached(
            "EnterActionProvider",
            typeof(IEnterActionProvider),
            typeof(DreamineVirtualKeyboardAssist),
            new PropertyMetadata(null, OnEnterActionProviderChanged));

    /// <summary>
    /// \if KO
    /// <para>지정한 객체의 Enter 동작 공급자를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the Enter-action provider from an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>조회 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>연결된 공급자이며 없으면 null입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The attached provider, or null if absent.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static IEnterActionProvider? GetEnterActionProvider(DependencyObject obj)
        => (IEnterActionProvider?)obj.GetValue(EnterActionProviderProperty);

    /// <summary>
    /// \if KO
    /// <para>지정한 객체에 Enter 동작 공급자를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attaches an Enter-action provider to an object.</para>
    /// \endif
    /// </summary>
    /// <param name="obj">
    /// \if KO
    /// <para>설정 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target object.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>연결할 공급자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The provider to attach.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="obj"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="obj"/> is null.</para>
    /// \endif
    /// </exception>
    public static void SetEnterActionProvider(DependencyObject obj, IEnterActionProvider value)
        => obj.SetValue(EnterActionProviderProperty, value);

    /// <summary>
    /// \if KO
    /// <para>새 Enter 동작 공급자에 연결 속성 소유자를 배치 대상으로 지정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Assigns the attached-property owner as the placement target of a new Enter-action provider.</para>
    /// \endif
    /// </summary>
    /// <param name="d">
    /// \if KO
    /// <para>공급자를 소유한 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that owns the provider.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>새 공급자를 포함하는 변경 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Change data containing the new provider.</para>
    /// \endif
    /// </param>
    private static void OnEnterActionProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is IEnterActionProvider provider)
        {
            provider.PlacementTarget = d;
        }
    }
    #endregion

    #region

    /// <summary>
    /// \if KO
    /// <para>기본 레이아웃 조회 및 바인딩 확장 동작을 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Registers default layout-resolution and binding extension actions.</para>
    /// \endif
    /// </summary>
    static DreamineVirtualKeyboardAssist()
    {
        SetBindingAction = SetBinding;
        GetKeyboardLayoutAction = GetLayout;
    }

    #endregion


    #region Event Handlers

    /// <summary>
    /// \if KO
    /// <para>가상 키보드 사용 설정에 따라 입력·활성·표시 이벤트 처리기를 연결하거나 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Hooks or unhooks input, enabled-state, and visibility handlers according to keyboard usage.</para>
    /// \endif
    /// </summary>
    /// <param name="d">
    /// \if KO
    /// <para>설정이 변경된 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object whose setting changed.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>새 사용 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The new usage state.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>입력 요소가 비활성화되거나 히트 테스트 불가가 되면 표시 중인 키보드를 숨깁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Hides the visible keyboard when an input becomes disabled or non-hit-testable.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>상태가 변경된 입력 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input whose state changed.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>새 bool 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The new Boolean state.</para>
    /// \endif
    /// </param>
    /// <exception cref="InvalidCastException">
    /// \if KO
    /// <para>새 값이 bool이 아닐 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the new value is not a Boolean.</para>
    /// \endif
    /// </exception>
    private static void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isEnable = (bool)e.NewValue;

        if (!isEnable && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>입력 요소를 누르면 해당 요소의 가상 키보드 표시를 전환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Toggles virtual-keyboard display when an input element is pressed.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>눌린 입력 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The pressed input element.</para>
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
    private static void OnControlMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            ToggleVirtualKeyBoard(d);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>입력 요소가 숨겨지면 표시 중인 키보드를 숨깁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Hides the visible keyboard when its input element becomes invisible.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>표시 상태가 변경된 입력 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input whose visibility changed.</para>
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
    /// <para>새 값이 bool이 아닐 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the new value is not a Boolean.</para>
    /// \endif
    /// </exception>
    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isVisible = (bool)e.NewValue;
        if (!isVisible && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>전역 설정, 현재 대상 및 읽기 전용 상태에 따라 키보드를 숨기거나 표시합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Hides or shows the keyboard according to global enablement, current target, and read-only state.</para>
    /// \endif
    /// </summary>
    /// <param name="placementTarget">
    /// \if KO
    /// <para>전환을 요청한 입력 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input target requesting the toggle.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>지정한 대상이 현재 키보드 배치 대상과 다른지 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether a target differs from the current keyboard placement target.</para>
    /// \endif
    /// </summary>
    /// <param name="placementTarget">
    /// \if KO
    /// <para>비교할 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target to compare.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>대상이 변경되었으면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> if the target changed.</para>
    /// \endif
    /// </returns>
    public static bool TargetChanged(DependencyObject? placementTarget)
    {
        return _placementTarget != placementTarget;
    }

    /// <summary>
    /// \if KO
    /// <para>대상 레이아웃과 바인딩을 구성하고 공유 가상 키보드 창을 표시·활성화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Configures target layout and binding, then shows and activates the shared virtual-keyboard window.</para>
    /// \endif
    /// </summary>
    /// <param name="placementTarget">
    /// \if KO
    /// <para>편집할 입력 대상이며 null이면 무시됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input target to edit; null is ignored.</para>
    /// \endif
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>이미 닫힌 공유 창을 표시하려 할 때 WPF에서 발생할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>May be thrown by WPF when attempting to show a shared window that has been closed.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>대상의 장식자 레이어에 가상 키보드 아이콘을 추가하거나 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds or removes the virtual-keyboard icon from the target's adorner layer.</para>
    /// \endif
    /// </summary>
    /// <param name="placementTarget">
    /// \if KO
    /// <para>아이콘 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The icon target.</para>
    /// \endif
    /// </param>
    /// <param name="visible">
    /// \if KO
    /// <para>표시하려면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> to show the icon.</para>
    /// \endif
    /// </param>
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
    	
	/// <summary>
	/// \if KO
	/// <para>대상 형식과 레이아웃에 맞춰 텍스트·숫자·소수·암호 값을 가상 키보드 창에 바인딩합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Binds text, integer, decimal, or password values to the virtual-keyboard window according to target and layout.</para>
	/// \endif
	/// </summary>
	/// <param name="placementTarget">
	/// \if KO
	/// <para>바인딩할 입력 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The input target to bind.</para>
	/// \endif
	/// </param>
	/// <param name="layout">
	/// \if KO
	/// <para>적용할 키보드 레이아웃입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The keyboard layout to apply.</para>
	/// \endif
	/// </param>
	/// <param name="virtualKeyboardWindow">
	/// \if KO
	/// <para>바인딩을 받을 키보드 창입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The keyboard window receiving the binding.</para>
	/// \endif
	/// </param>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="placementTarget"/> 또는 <paramref name="virtualKeyboardWindow"/>가 null일 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when <paramref name="placementTarget"/> or <paramref name="virtualKeyboardWindow"/> is null.</para>
	/// \endif
	/// </exception>
	/// <exception cref="OverflowException">
	/// \if KO
	/// <para>구성된 decimal 최소·최대값을 int로 변환할 수 없을 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when configured decimal bounds cannot be converted to integers.</para>
	/// \endif
	/// </exception>
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

	/// <summary>
	/// \if KO
	/// <para>대상의 문자열 Password CLR 또는 종속성 속성을 찾아 양방향 바인딩을 만듭니다.</para>
	/// \endif
	/// \if EN
	/// <para>Finds a string Password CLR or dependency property and creates a two-way binding.</para>
	/// \endif
	/// </summary>
	/// <param name="target">
	/// \if KO
	/// <para>검사할 입력 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The input target to inspect.</para>
	/// \endif
	/// </param>
	/// <param name="binding">
	/// \if KO
	/// <para>성공 시 생성된 암호 바인딩입니다.</para>
	/// \endif
	/// \if EN
	/// <para>On success, the created password binding.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>호환 Password 속성을 찾았으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if a compatible Password property was found.</para>
	/// \endif
	/// </returns>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="target"/>이 null일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="target"/> is null.</para>
	/// \endif
	/// </exception>
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

	/// <summary>
	/// \if KO
	/// <para>대상에 읽고 쓸 수 있는 문자열 Password CLR 속성 또는 Password 종속성 속성이 있는지 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether a target exposes a readable/writable string Password CLR property or Password dependency property.</para>
	/// \endif
	/// </summary>
	/// <param name="target">
	/// \if KO
	/// <para>검사할 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The target to inspect.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>호환 Password 속성이 있으면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if a compatible Password property exists.</para>
	/// \endif
	/// </returns>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="target"/>이 null일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="target"/> is null.</para>
	/// \endif
	/// </exception>
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

	/// <summary>
	/// \if KO
	/// <para>공유 키보드 창과 현재 배치 대상 참조를 초기화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Clears references to the shared keyboard window and current placement target.</para>
	/// \endif
	/// </summary>
	/// <remarks>
	/// \if KO
	/// <para>창을 닫거나 해제하지 않으므로 정상 종료에는 <see cref="Shutdown"/>을 사용합니다.</para>
	/// \endif
	/// \if EN
	/// <para>This does not close or dispose the window; use <see cref="Shutdown"/> for normal shutdown.</para>
	/// \endif
	/// </remarks>
	public static void ResetDreamineVirtualKeyboard()
    {
        _virtualKeyboardWindow = null!;
        _placementTarget = null;
    }

	/// <summary>
	/// \if KO
	/// <para>공유 창의 가상 키보드를 해제하고 창을 닫은 뒤 전역 참조를 초기화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Disposes the virtual keyboard in the shared window, closes the window, and clears global references.</para>
	/// \endif
	/// </summary>
	/// <remarks>
	/// \if KO
	/// <para>애플리케이션 종료 또는 주 창 닫힘 시 호출하며 정리 중 예외는 의도적으로 무시합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Call during application exit or main-window closure; cleanup exceptions are intentionally ignored.</para>
	/// \endif
	/// </remarks>
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

	/// <summary>
	/// \if KO
	/// <para>시각적 트리를 재귀 탐색해 발견한 첫 가상 키보드를 해제합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Recursively traverses the visual tree and disposes the first virtual keyboard found.</para>
	/// \endif
	/// </summary>
	/// <param name="root">
	/// \if KO
	/// <para>탐색 시작 객체입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The traversal root.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// \if KO
	/// <para>루트가 유효한 시각적 객체가 아닐 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when the root is not a valid visual object.</para>
	/// \endif
	/// </exception>
	private static void DisposeVkControl(System.Windows.DependencyObject root)
	{
		if (root is DreamineVirtualKeyboard vk) { vk.Dispose(); return; }
		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
			DisposeVkControl(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
	}

    /// <summary>
    /// \if KO
    /// <para>대상 자체 및 가장 가까운 Enter 그룹에서 실행할 공급자를 순서대로 수집합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Collects providers to execute from the target and nearest Enter-action group in order.</para>
    /// \endif
    /// </summary>
    /// <param name="d">
    /// \if KO
    /// <para>공급자를 조회할 배치 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The placement target whose providers are queried.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>직접 공급자, 그룹 하위 공급자 및 확정 공급자 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A list containing the direct provider, group descendants, and commit provider.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>대상이 유효한 시각적 트리에 속하지 않을 때 부모 탐색에서 발생할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>May be thrown during parent traversal when the target is not in a valid visual tree.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>제외 대상 이외의 시각적 후손에 연결된 Enter 동작 공급자를 재귀 열거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Recursively enumerates Enter-action providers attached to visual descendants except excluded objects.</para>
    /// \endif
    /// </summary>
    /// <param name="root">
    /// \if KO
    /// <para>탐색 루트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The traversal root.</para>
    /// \endif
    /// </param>
    /// <param name="excepts">
    /// \if KO
    /// <para>공급자 조회에서 제외할 객체 배열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Objects excluded from provider lookup.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>연결된 공급자의 지연 시퀀스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A deferred sequence of attached providers.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NullReferenceException">
    /// \if KO
    /// <para><paramref name="excepts"/>가 null일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="excepts"/> is null.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>리플렉션으로 대상의 선택적 IsReadOnly 속성 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets an optional IsReadOnly property value from a target through reflection.</para>
    /// \endif
    /// </summary>
    /// <param name="placementTarget">
    /// \if KO
    /// <para>검사할 입력 대상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input target to inspect.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>읽기 전용 상태이며 속성이 없으면 null입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The read-only state, or null if the property is absent.</para>
    /// \endif
    /// </returns>
    /// <exception cref="System.Reflection.TargetInvocationException">
    /// \if KO
    /// <para>IsReadOnly getter가 예외를 발생시키면 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the IsReadOnly getter throws.</para>
    /// \endif
    /// </exception>
    private static bool? IsReadOnly(DependencyObject placementTarget)
    {
        var prop = placementTarget.GetType().GetProperty("IsReadOnly");
        return (bool?)prop?.GetValue(placementTarget);
    }

    #endregion
}

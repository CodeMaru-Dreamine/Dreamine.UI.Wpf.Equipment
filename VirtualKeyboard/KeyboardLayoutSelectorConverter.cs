using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>가상 키보드 레이아웃 값에 맞는 데이터 템플릿을 선택합니다.</para>
/// \endif
/// \if EN
/// <para>Selects a data template for a virtual-keyboard layout value.</para>
/// \endif
/// </summary>
public class KeyboardLayoutSelectorConverter : IValueConverter
{
    /// <summary>
    /// \if KO
    /// <para>일반 키보드에 사용할 기본 템플릿을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the default template used for a regular keyboard.</para>
    /// \endif
    /// </summary>
    public DataTemplate? DefaultTemplate { get; set; }
    /// <summary>
    /// \if KO
    /// <para>숫자 키패드에 사용할 템플릿을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the template used for the numeric keypad.</para>
    /// \endif
    /// </summary>
    public DataTemplate? NumPadTemplate { get; set; }

    /// <summary>
    /// \if KO
    /// <para>숫자 레이아웃이면 숫자 템플릿을, 아니면 기본 템플릿을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns the numeric template for a numeric layout; otherwise returns the default template.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>변환할 레이아웃 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The layout value to convert.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>바인딩 대상 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The binding target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>선택적 변환기 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The optional converter parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>변환 문화권입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The conversion culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>선택된 데이터 템플릿이며 구성되지 않았으면 <see langword="null"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The selected data template, or <see langword="null"/> if not configured.</para>
    /// \endif
    /// </returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VkLayout layout && layout == VkLayout.Numeric)
            return NumPadTemplate;
        return DefaultTemplate;
    }

    /// <summary>
    /// \if KO
    /// <para>역변환은 지원하지 않으며 항상 예외를 발생시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reverse conversion is unsupported and always throws.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>역변환할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to convert back.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>대상 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>변환기 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The converter parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>변환 문화권입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The conversion culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>정상적으로 반환하지 않습니다.</para>
    /// \endif
    /// \if EN
    /// <para>This method never returns normally.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// \if KO
    /// <para>호출될 때 항상 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Always thrown when called.</para>
    /// \endif
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

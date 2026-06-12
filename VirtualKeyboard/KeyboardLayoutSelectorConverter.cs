using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public class KeyboardLayoutSelectorConverter : IValueConverter
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? NumPadTemplate { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is eVkLayout layout && layout == eVkLayout.Numeric)
            return NumPadTemplate;
        return DefaultTemplate;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

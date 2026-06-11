using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public class DreamineVkbIconAdorner : Adorner
{
    private readonly Path _iconPath;

    public DreamineVkbIconAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _iconPath = new Path
        {
            Data = Geometry.Parse("M320-280h320v-80H320v80ZM200-400h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80ZM160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm0-440h640v-120H160v120Zm0 360h640v-280H160v280Zm0 0v-280 280Z"),
            Fill = Brushes.Red,
            Stretch = Stretch.Uniform,
            Focusable = false
        };

        AddVisualChild(_iconPath);
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _iconPath;

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (AdornedElement is FrameworkElement target)
        {
            _iconPath.Width = target.ActualHeight;
            _iconPath.Height = target.ActualHeight;

            double iconWidth = _iconPath.Width;
            double iconHeight = _iconPath.Height;

            double x = target.ActualWidth - iconWidth;
            double y = target.ActualHeight - iconHeight;

            _iconPath.Arrange(new Rect(x, y, iconWidth, iconHeight));
        }

        return finalSize;
    }

    public void SetPreviewMouseDownAction(Action<object, MouseButtonEventArgs> previewMouseDownAction)
    {
        _iconPath.PreviewMouseDown += new MouseButtonEventHandler((s, e) => previewMouseDownAction(s, e));
    }
}


using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public sealed class DreamineVkbIconAdorner : Adorner
{
    private Action<object?, MouseButtonEventArgs>? _previewMouseDownAction;

    public DreamineVkbIconAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = true;
        Cursor = Cursors.Hand;
        PreviewMouseDown += OnPreviewMouseDown;
    }

    public void SetPreviewMouseDownAction(Action<object?, MouseButtonEventArgs> action)
        => _previewMouseDownAction = action;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var adornedSize = AdornedElement.RenderSize;
        const double width = 34;
        const double height = 22;
        var rect = new Rect(
            Math.Max(0, adornedSize.Width - width - 4),
            Math.Max(0, adornedSize.Height - height - 4),
            width,
            height);

        var background = new SolidColorBrush(Color.FromArgb(230, 45, 52, 64));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(120, 140, 160)), 1);
        drawingContext.DrawRoundedRectangle(background, border, rect, 4, 4);

        var keyBrush = new SolidColorBrush(Color.FromRgb(230, 236, 244));
        const double keySize = 4;
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 5; col++)
            {
                var keyRect = new Rect(rect.Left + 5 + col * 5, rect.Top + 5 + row * 6, keySize, keySize);
                drawingContext.DrawRoundedRectangle(keyBrush, null, keyRect, 1, 1);
            }
        }

        drawingContext.DrawRoundedRectangle(
            keyBrush,
            null,
            new Rect(rect.Left + 8, rect.Top + 17, 18, 2),
            1,
            1);
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _previewMouseDownAction?.Invoke(sender, e);
        e.Handled = true;
    }
}

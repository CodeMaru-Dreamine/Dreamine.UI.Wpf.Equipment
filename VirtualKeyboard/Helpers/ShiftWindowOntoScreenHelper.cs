using System.Windows;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public static class ShiftWindowOntoScreenHelper
{

    public static void ShiftWindowOntoScreen(Window window, Rect waDip)
    {
        double width = double.IsNaN(window.Width) || window.Width == 0 ? window.ActualWidth : window.Width;
        double height = double.IsNaN(window.Height) || window.Height == 0 ? window.ActualHeight : window.Height;

        double left = window.Left;
        double top = window.Top;

        if (left + width > waDip.Right) left = waDip.Right - width;
        if (left < waDip.Left) left = waDip.Left;
        if (top + height > waDip.Bottom) top = waDip.Bottom - height;
        if (top < waDip.Top) top = waDip.Top;

        window.Left = left;
        window.Top = top;
    }
}

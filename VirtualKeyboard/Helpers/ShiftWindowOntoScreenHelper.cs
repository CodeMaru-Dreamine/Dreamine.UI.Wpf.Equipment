using System.Windows;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>창의 위치를 지정한 화면 작업 영역 안으로 보정합니다.</para>
/// \endif
/// \if EN
/// <para>Adjusts a window position so it remains within a specified screen work area.</para>
/// \endif
/// </summary>
public static class ShiftWindowOntoScreenHelper
{

    /// <summary>
    /// \if KO
    /// <para>창 크기를 고려하여 좌표를 작업 영역 경계 안으로 이동합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Moves window coordinates inside work-area bounds while accounting for window size.</para>
    /// \endif
    /// </summary>
    /// <param name="window">
    /// \if KO
    /// <para>위치를 보정할 창입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The window whose position is adjusted.</para>
    /// \endif
    /// </param>
    /// <param name="waDip">
    /// \if KO
    /// <para>장치 독립 단위의 허용 작업 영역입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The allowed work area in device-independent units.</para>
    /// \endif
    /// </param>
    /// <exception cref="NullReferenceException">
    /// \if KO
    /// <para><paramref name="window"/>가 <see langword="null"/>일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="window"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
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

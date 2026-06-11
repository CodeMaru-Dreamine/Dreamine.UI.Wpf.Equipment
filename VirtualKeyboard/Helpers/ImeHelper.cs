namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public static class ImeHelper
{
    private const uint IME_CMODE_NATIVE = 0x0001;

    public static bool GetImeMode()
    {
        IntPtr hwnd = Win32Api.GetForegroundWindow();
        IntPtr hIMC = Win32Api.ImmGetContext(hwnd);

        if (hIMC == IntPtr.Zero)
            return false;

        bool isOpen = Win32Api.ImmGetOpenStatus(hIMC);
        if (!isOpen)
        {
            Win32Api.ImmReleaseContext(hwnd, hIMC);
            return false;
        }

        Win32Api.ImmGetConversionStatus(hIMC, out uint conv, out _);
        Win32Api.ImmReleaseContext(hwnd, hIMC);

        return (conv & IME_CMODE_NATIVE) != 0
            ? true
            : false;
    }
}

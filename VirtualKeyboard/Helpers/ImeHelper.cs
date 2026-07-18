namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>현재 전경 창의 Windows IME 한영 입력 모드를 조회하거나 변경합니다.</para>
/// \endif
/// \if EN
/// <para>Gets or changes the Windows IME native-input mode for the foreground window.</para>
/// \endif
/// </summary>
public static class ImeHelper
{
    /// <summary>
    /// \if KO
    /// <para>IME 네이티브 변환 모드 비트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Specifies the IME native-conversion mode bit.</para>
    /// \endif
    /// </summary>
    private const uint IME_CMODE_NATIVE = 0x0001;

    /// <summary>
    /// \if KO
    /// <para>현재 전경 창의 IME가 열려 있고 네이티브 변환 모드인지 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether the foreground window's IME is open in native-conversion mode.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>네이티브 입력 모드이면 <see langword="true"/>이며 컨텍스트가 없거나 닫혔으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> for native-input mode; <see langword="false"/> if the context is absent or closed.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>현재 전경 창의 IME 열림 및 네이티브 변환 모드를 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the foreground window's IME open state and native-conversion mode.</para>
    /// \endif
    /// </summary>
    /// <param name="native">
    /// \if KO
    /// <para>네이티브 입력을 활성화하려면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> to enable native input.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>IME 컨텍스트를 얻어 설정을 시도했으면 <see langword="true"/>이고 컨텍스트가 없으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> if an IME context was obtained and setting was attempted; otherwise <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public static bool SetImeMode(bool native)
    {
        IntPtr hwnd = Win32Api.GetForegroundWindow();
        IntPtr hIMC = Win32Api.ImmGetContext(hwnd);

        if (hIMC == IntPtr.Zero)
            return false;

        try
        {
            Win32Api.ImmSetOpenStatus(hIMC, native);

            if (Win32Api.ImmGetConversionStatus(hIMC, out uint conv, out uint sentence))
            {
                conv = native ? conv | IME_CMODE_NATIVE : conv & ~IME_CMODE_NATIVE;
                Win32Api.ImmSetConversionStatus(hIMC, conv, sentence);
            }

            return true;
        }
        finally
        {
            Win32Api.ImmReleaseContext(hwnd, hIMC);
        }
    }
}

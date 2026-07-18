using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>Windows 후크 체인에서 호출되는 콜백을 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Represents a callback invoked by a Windows hook chain.</para>
/// \endif
/// </summary>
/// <param name="nCode">
/// \if KO
/// <para>후크 처리 코드입니다.</para>
/// \endif
/// \if EN
/// <para>The hook-processing code.</para>
/// \endif
/// </param>
/// <param name="wParam">
/// \if KO
/// <para>후크별 메시지 매개변수입니다.</para>
/// \endif
/// \if EN
/// <para>The hook-specific message parameter.</para>
/// \endif
/// </param>
/// <param name="lParam">
/// \if KO
/// <para>후크별 추가 매개변수입니다.</para>
/// \endif
/// \if EN
/// <para>The hook-specific additional parameter.</para>
/// \endif
/// </param>
/// <returns>
/// \if KO
/// <para>후크 처리 결과입니다.</para>
/// \endif
/// \if EN
/// <para>The hook-processing result.</para>
/// \endif
/// </returns>
public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

/// <summary>
/// \if KO
/// <para>키보드 후크 및 IME 제어에 필요한 Win32 네이티브 함수 선언을 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides Win32 native declarations required for keyboard hooks and IME control.</para>
/// \endif
/// </summary>
public static class Win32Api
{
    /// <summary>
    /// \if KO
    /// <para>지정한 형식의 Windows 후크를 설치합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Installs a Windows hook of the specified type.</para>
    /// \endif
    /// </summary>
    /// <param name="idHook">
    /// \if KO
    /// <para>후크 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The hook type.</para>
    /// \endif
    /// </param>
    /// <param name="lpfn">
    /// \if KO
    /// <para>후크 콜백입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The hook callback.</para>
    /// \endif
    /// </param>
    /// <param name="hMod">
    /// \if KO
    /// <para>콜백을 포함한 모듈 핸들입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The module handle containing the callback.</para>
    /// \endif
    /// </param>
    /// <param name="dwThreadId">
    /// \if KO
    /// <para>대상 스레드 ID이며 0은 전체 스레드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target thread ID, or zero for all threads.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>후크 핸들이며 실패하면 0입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The hook handle, or zero on failure.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    /// <summary>
    /// \if KO
    /// <para>Unhook Windows Hook Ex 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unhook windows hook ex operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hhk">
    /// \if KO
    /// <para>hhk에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for hhk.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unhook Windows Hook Ex 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the unhook windows hook ex condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// \if KO
    /// <para>Call Next Hook Ex 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the call next hook ex operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hhk">
    /// \if KO
    /// <para>hhk에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for hhk.</para>
    /// \endif
    /// </param>
    /// <param name="nCode">
    /// \if KO
    /// <para>n Code에 사용할 <see cref="int"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="int"/> value used for n code.</para>
    /// \endif
    /// </param>
    /// <param name="wParam">
    /// \if KO
    /// <para>w Param에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for w param.</para>
    /// \endif
    /// </param>
    /// <param name="lParam">
    /// \if KO
    /// <para>l Param에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for l param.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Call Next Hook Ex 작업에서 생성한 <see cref="IntPtr"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> result produced by the call next hook ex operation.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// \if KO
    /// <para>Foreground Window 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the foreground window value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Foreground Window 작업에서 생성한 <see cref="IntPtr"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> result produced by the get foreground window operation.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// \if KO
    /// <para>Window Thread Process Id 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the window thread process id value.</para>
    /// \endif
    /// </summary>
    /// <param name="hWnd">
    /// \if KO
    /// <para>h Wnd에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h wnd.</para>
    /// \endif
    /// </param>
    /// <param name="ProcessId">
    /// \if KO
    /// <para>Process Id에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for process id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Window Thread Process Id 작업에서 생성한 <see cref="uint"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> result produced by the get window thread process id operation.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    /// <summary>
    /// \if KO
    /// <para>Keyboard Layout 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the keyboard layout value.</para>
    /// \endif
    /// </summary>
    /// <param name="idThread">
    /// \if KO
    /// <para>id Thread에 사용할 <see cref="uint"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> value used for id thread.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Keyboard Layout 작업에서 생성한 <see cref="IntPtr"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> result produced by the get keyboard layout operation.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>
    /// \if KO
    /// <para>Current Thread Id 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current thread id value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Current Thread Id 작업에서 생성한 <see cref="uint"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> result produced by the get current thread id operation.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    /// <summary>
    /// \if KO
    /// <para>Module Handle 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the module handle value.</para>
    /// \endif
    /// </summary>
    /// <param name="lpModuleName">
    /// \if KO
    /// <para>lp Module Name에 사용할 <see cref="string"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="string"/> value used for lp module name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Module Handle 작업에서 생성한 <see cref="IntPtr"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> result produced by the get module handle operation.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// \if KO
    /// <para>Keyboard Layout Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the keyboard layout name value.</para>
    /// \endif
    /// </summary>
    /// <param name="pwszKLID">
    /// \if KO
    /// <para>pwsz KLID에 사용할 <see cref="StringBuilder"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="StringBuilder"/> value used for pwsz klid.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Keyboard Layout Name 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the get keyboard layout name condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetKeyboardLayoutName([Out] StringBuilder pwszKLID);

    /// <summary>
    /// \if KO
    /// <para>Imm Get Context 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm get context operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hWnd">
    /// \if KO
    /// <para>h Wnd에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h wnd.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Get Context 작업에서 생성한 <see cref="IntPtr"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> result produced by the imm get context operation.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmGetContext(IntPtr hWnd);

    /// <summary>
    /// \if KO
    /// <para>Imm Release Context 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm release context operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hWnd">
    /// \if KO
    /// <para>h Wnd에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h wnd.</para>
    /// \endif
    /// </param>
    /// <param name="hIMC">
    /// \if KO
    /// <para>h IMC에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h imc.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Release Context 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the imm release context condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    /// <summary>
    /// \if KO
    /// <para>Imm Get Open Status 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm get open status operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hIMC">
    /// \if KO
    /// <para>h IMC에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h imc.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Get Open Status 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the imm get open status condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern bool ImmGetOpenStatus(IntPtr hIMC);

    /// <summary>
    /// \if KO
    /// <para>Imm Set Open Status 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm set open status operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hIMC">
    /// \if KO
    /// <para>h IMC에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h imc.</para>
    /// \endif
    /// </param>
    /// <param name="fOpen">
    /// \if KO
    /// <para>f Open에 사용할 <see cref="bool"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="bool"/> value used for f open.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Set Open Status 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the imm set open status condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern bool ImmSetOpenStatus(IntPtr hIMC, bool fOpen);

    /// <summary>
    /// \if KO
    /// <para>Imm Get Conversion Status 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm get conversion status operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hIMC">
    /// \if KO
    /// <para>h IMC에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h imc.</para>
    /// \endif
    /// </param>
    /// <param name="pdwConversion">
    /// \if KO
    /// <para>pdw Conversion에 사용할 <see cref="uint"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> value used for pdw conversion.</para>
    /// \endif
    /// </param>
    /// <param name="pdwSentence">
    /// \if KO
    /// <para>pdw Sentence에 사용할 <see cref="uint"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> value used for pdw sentence.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Get Conversion Status 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the imm get conversion status condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern bool ImmGetConversionStatus(IntPtr hIMC, out uint pdwConversion, out uint pdwSentence);

    /// <summary>
    /// \if KO
    /// <para>Imm Set Conversion Status 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the imm set conversion status operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hIMC">
    /// \if KO
    /// <para>h IMC에 사용할 <see cref="IntPtr"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="IntPtr"/> value used for h imc.</para>
    /// \endif
    /// </param>
    /// <param name="fdwConversion">
    /// \if KO
    /// <para>fdw Conversion에 사용할 <see cref="uint"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> value used for fdw conversion.</para>
    /// \endif
    /// </param>
    /// <param name="fdwSentence">
    /// \if KO
    /// <para>fdw Sentence에 사용할 <see cref="uint"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="uint"/> value used for fdw sentence.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Imm Set Conversion Status 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the imm set conversion status condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("imm32.dll")]
    public static extern bool ImmSetConversionStatus(IntPtr hIMC, uint fdwConversion, uint fdwSentence);

    /// <summary>
    /// \if KO
    /// <para>Key State 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the key state value.</para>
    /// \endif
    /// </summary>
    /// <param name="keyCode">
    /// \if KO
    /// <para>key Code에 사용할 <see cref="int"/> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="int"/> value used for key code.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Key State 작업에서 생성한 <see cref="short"/> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <see cref="short"/> result produced by the get key state operation.</para>
    /// \endif
    /// </returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern short GetKeyState(int keyCode);
}

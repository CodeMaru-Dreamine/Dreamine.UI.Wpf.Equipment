using SharpHook.Native;
using System.Windows;
using System.Windows.Controls;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>키 코드와 입력 상태에 따라 다국어 표시 문자를 갱신하는 가상 키 버튼입니다.</para>
/// \endif
/// \if EN
/// <para>Represents a virtual-key button that updates multilingual display text from a key code and input state.</para>
/// \endif
/// </summary>
public class Key : Button
{
    #region Variable

    /// <summary>
    /// \if KO
    /// <para>키 코드별 기본·Shift·언어별 표시 문자 매핑을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores default, Shift, and language-specific display mappings by key code.</para>
    /// \endif
    /// </summary>
    private static Dictionary<KeyCode, KeyData> _dicKeyData = new();

    #endregion

    #region Dependency Property

    #region IsPressed

    /// <summary>
    /// \if KO
    /// <para>가상 키 눌림 상태 종속성 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the virtual-key pressed-state dependency property.</para>
    /// \endif
    /// </summary>
    public static readonly new DependencyProperty IsPressedProperty =
        DependencyProperty.Register(nameof(IsPressed),
            typeof(bool),
            typeof(Key));

    /// <summary>
    /// \if KO
    /// <para>가상 키가 눌린 상태인지 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the virtual key is pressed.</para>
    /// \endif
    /// </summary>
    public new bool IsPressed
    {
        get { return (bool)GetValue(IsPressedProperty); }
        set { SetValue(IsPressedProperty, value); }
    }

    #endregion

    #region KeyCode

    /// <summary>
    /// \if KO
    /// <para>네이티브 키 코드 종속성 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the native key-code dependency property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty KeyCodeProperty =
        DependencyProperty.Register(nameof(KeyCode), typeof(KeyCode), typeof(Key));

    /// <summary>
    /// \if KO
    /// <para>버튼이 나타내는 네이티브 키 코드를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the native key code represented by the button.</para>
    /// \endif
    /// </summary>
    public KeyCode KeyCode
    {
        get { return (KeyCode)GetValue(KeyCodeProperty); }
        set { SetValue(KeyCodeProperty, value); }
    }

    #endregion Public Method

    /// <summary>
    /// \if KO
    /// <para>Shift·Caps Lock·언어·IME 상태에 맞는 표시 문자로 버튼 콘텐츠를 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Updates button content for the current Shift, Caps Lock, language, and IME state.</para>
    /// \endif
    /// </summary>
    /// <param name="shift">
    /// \if KO
    /// <para>Shift 활성 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Shift state.</para>
    /// \endif
    /// </param>
    /// <param name="capsLock">
    /// \if KO
    /// <para>Caps Lock 활성 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Caps Lock state.</para>
    /// \endif
    /// </param>
    /// <param name="languageCode">
    /// \if KO
    /// <para>표시 언어입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The display language.</para>
    /// \endif
    /// </param>
    /// <param name="imeMode">
    /// \if KO
    /// <para>IME 언어 표시를 사용하려면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> to use IME-language display.</para>
    /// \endif
    /// </param>
    public void UpdateKey(bool shift, bool capsLock, LanguageCode languageCode, bool imeMode)
    {
        if (!_dicKeyData.TryGetValue(KeyCode, out var keyData)) return;

        Content = GetDisplayText(shift, capsLock, languageCode, imeMode);
    }

    /// <summary>
    /// \if KO
    /// <para>현재 키와 입력 상태에 사용할 표시 문자열을 계산합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Computes the display string for the current key and input state.</para>
    /// \endif
    /// </summary>
    /// <param name="shift">
    /// \if KO
    /// <para>Shift 활성 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Shift state.</para>
    /// \endif
    /// </param>
    /// <param name="capsLock">
    /// \if KO
    /// <para>Caps Lock 활성 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Caps Lock state.</para>
    /// \endif
    /// </param>
    /// <param name="languageCode">
    /// \if KO
    /// <para>표시 언어입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The display language.</para>
    /// \endif
    /// </param>
    /// <param name="imeMode">
    /// \if KO
    /// <para>IME 언어 매핑을 적용하려면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> to apply IME-language mapping.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>상태에 맞는 표시 문자열이며 매핑이 없으면 빈 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The state-specific display string, or an empty string when no mapping exists.</para>
    /// \endif
    /// </returns>
    public string GetDisplayText(bool shift, bool capsLock, LanguageCode languageCode, bool imeMode)
    {
        if (!_dicKeyData.TryGetValue(KeyCode, out var keyData))
            return string.Empty;

        var (displayKey, displayShiftKey) = GetKeyData(imeMode ? languageCode : LanguageCode.en_US);

        if (KeyCode >= KeyCode.VcA && KeyCode <= KeyCode.VcZ)
        {
            if (shift && !capsLock)
            {
                if (!string.IsNullOrEmpty(displayShiftKey))
                {
                    displayKey = displayShiftKey.ToUpper();
                }
                else
                {
                    displayKey = displayKey.ToUpper();
                }
            }
            else if (!shift && capsLock)
            {
                displayKey = displayKey.ToUpper();
            }
            else if (shift && capsLock)
            {
                if (!string.IsNullOrEmpty(displayShiftKey))
                {
                    displayKey = displayShiftKey.ToLower();
                }
                else
                {
                    displayKey = displayKey.ToLower();
                }
            }
        }
        else if (shift && !string.IsNullOrEmpty(keyData.ShiftKey))
        {
            displayKey = keyData.ShiftKey;
        }

        return displayKey;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// \if KO
    /// <para>모든 지원 키 코드의 표시 문자 매핑을 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes display mappings for all supported key codes.</para>
    /// \endif
    /// </summary>
    static Key()
    {
        MappingKeys();
    }

    /// <summary>
    /// \if KO
    /// <para>포커스를 받지 않고 누르는 즉시 클릭되는 새 가상 키를 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a nonfocusable virtual key that clicks on press.</para>
    /// \endif
    /// </summary>
    public Key()
    {
        Focusable = false;
        IsTabStop = false;
        ClickMode = ClickMode.Press;
    }

    #endregion


    #region Private Method
    /// <summary>
    /// \if KO
    /// <para>지정한 언어에 해당하는 기본 및 Shift 표시 문자를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the default and Shift display strings for a language.</para>
    /// \endif
    /// </summary>
    /// <param name="language">
    /// \if KO
    /// <para>조회할 언어입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The language to query.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>기본 표시 문자와 Shift 표시 문자 쌍입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A pair containing default and Shift display strings.</para>
    /// \endif
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// \if KO
    /// <para>현재 <see cref="KeyCode"/>에 대한 매핑이 없을 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when no mapping exists for the current <see cref="KeyCode"/>.</para>
    /// \endif
    /// </exception>
    private (string, string) GetKeyData(LanguageCode language)
    {
        var keyData = _dicKeyData[KeyCode];

        var key = language switch
        {
            LanguageCode.en_US => keyData.DefaultKey,
            LanguageCode.ko_KR => keyData.KorKey,
            LanguageCode.zh_CN => keyData.ChnKey,
            _ => keyData.DefaultKey
        };

        var shiftKey = language switch
        {
            LanguageCode.en_US => keyData.ShiftKey,
            LanguageCode.ko_KR => keyData.KorShiftKey,
            LanguageCode.zh_CN => keyData.ChnShiftKey,
            _ => keyData.ShiftKey
        };

        if (string.IsNullOrEmpty(key))
        {
            key = keyData.DefaultKey;
        }

        return (key, shiftKey);
    }

    /// <summary>
    /// \if KO
    /// <para>지원하는 문자·기능·숫자 패드·화살표 키의 표시 매핑을 다시 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Rebuilds display mappings for supported character, function, numpad, and arrow keys.</para>
    /// \endif
    /// </summary>
    private static void MappingKeys()
    {
        _dicKeyData = new()
        {
            { KeyCode.Vc1, new(defaultKey: "1", shiftKey: "!" ) },
            { KeyCode.Vc2, new(defaultKey: "2", shiftKey: "@" ) },
            { KeyCode.Vc3, new(defaultKey: "3", shiftKey: "#" ) },
            { KeyCode.Vc4, new(defaultKey: "4", shiftKey: "$" ) },
            { KeyCode.Vc5, new(defaultKey: "5", shiftKey: "%" ) },
            { KeyCode.Vc6, new(defaultKey: "6", shiftKey: "^" ) },
            { KeyCode.Vc7, new(defaultKey: "7", shiftKey: "&" ) },
            { KeyCode.Vc8, new(defaultKey: "8", shiftKey: "*" ) },
            { KeyCode.Vc9, new(defaultKey: "9", shiftKey: "(" ) },
            { KeyCode.Vc0, new(defaultKey: "0", shiftKey: ")" ) },

            { KeyCode.VcA, new(defaultKey: "a", korKey: "ㅁ", chnKey: "日") },
            { KeyCode.VcB, new(defaultKey: "b", korKey: "ㅠ", chnKey: "月") },
            { KeyCode.VcC, new(defaultKey: "c", korKey: "ㅊ", chnKey: "金") },
            { KeyCode.VcD, new(defaultKey: "d", korKey: "ㅇ", chnKey: "木") },
            { KeyCode.VcE, new(defaultKey: "e", korKey: "ㄷ", korShiftKey: "ㄸ", chnKey: "水") },
            { KeyCode.VcF, new(defaultKey: "f", korKey: "ㄹ", chnKey: "火") },
            { KeyCode.VcG, new(defaultKey: "g", korKey: "ㅎ", chnKey: "土") },
            { KeyCode.VcH, new(defaultKey: "h", korKey: "ㅗ", chnKey: "竹") },
            { KeyCode.VcI, new(defaultKey: "i", korKey: "ㅑ", chnKey: "戈") },
            { KeyCode.VcJ, new(defaultKey: "j", korKey: "ㅓ", chnKey: "十") },
            { KeyCode.VcK, new(defaultKey: "k", korKey: "ㅏ", chnKey: "大") },
            { KeyCode.VcL, new(defaultKey: "l", korKey: "ㅣ", chnKey: "中") },
            { KeyCode.VcM, new(defaultKey: "m", korKey: "ㅡ", chnKey: "一") },
            { KeyCode.VcN, new(defaultKey: "n", korKey: "ㅜ", chnKey: "弓") },
            { KeyCode.VcO, new(defaultKey: "o", korKey: "ㅐ", korShiftKey: "ㅒ", chnKey: "人") },
            { KeyCode.VcP, new(defaultKey: "p", korKey: "ㅔ", korShiftKey : "ㅖ", chnKey: "心") },
            { KeyCode.VcQ, new(defaultKey: "q", korKey: "ㅂ", korShiftKey : "ㅃ", chnKey: "手") },
            { KeyCode.VcR, new(defaultKey: "r", korKey: "ㄱ", korShiftKey : "ㄲ", chnKey: "口") },
            { KeyCode.VcS, new(defaultKey: "s", korKey: "ㄴ", chnKey: "戶") },
            { KeyCode.VcT, new(defaultKey: "t", korKey: "ㅅ", korShiftKey: "ㅆ", chnKey: "甘") },
            { KeyCode.VcU, new(defaultKey: "u", korKey: "ㅕ", chnKey: "山") },
            { KeyCode.VcV, new(defaultKey: "v", korKey: "ㅍ", chnKey: "女") },
            { KeyCode.VcW, new(defaultKey: "w", korKey: "ㅈ", korShiftKey: "ㅉ", chnKey: "田") },
            { KeyCode.VcX, new(defaultKey: "x", korKey: "ㅌ", chnKey: "難") },
            { KeyCode.VcY, new(defaultKey: "y", korKey: "ㅛ", chnKey: "卜") },
            { KeyCode.VcZ, new(defaultKey: "z", korKey: "ㅋ", chnKey: "重") },

            { KeyCode.VcBackQuote, new(defaultKey: "`", shiftKey: "~") },
            { KeyCode.VcMinus, new(defaultKey: "-", shiftKey: "_") },
            { KeyCode.VcEquals, new(defaultKey: "=", shiftKey: "+") },
            { KeyCode.VcBackspace, new(defaultKey: "Backspace") },
            { KeyCode.VcTab, new(defaultKey: "Tab") },
            { KeyCode.VcOpenBracket, new(defaultKey: "[", shiftKey: "{") },
            { KeyCode.VcCloseBracket, new(defaultKey: "]", shiftKey: "}") },
            { KeyCode.VcBackslash, new(defaultKey: "\\", shiftKey: "|") },
            { KeyCode.VcCapsLock, new(defaultKey: "Caps Lock") },
            { KeyCode.VcSemicolon, new(defaultKey: ";", shiftKey: ":") },
            { KeyCode.VcQuote, new(defaultKey: "'", shiftKey: "″") },
            { KeyCode.VcEnter, new(defaultKey: "Enter") },
            { KeyCode.VcLeftShift, new(defaultKey: "Shift") },
            { KeyCode.VcComma, new(defaultKey: ",", shiftKey: "<") },
            { KeyCode.VcPeriod, new(defaultKey: ".", shiftKey: ">") },
            { KeyCode.VcSlash, new(defaultKey: "/", shiftKey: "?") },
            { KeyCode.VcSpace, new(defaultKey: "Space")},
            { KeyCode.VcRightAlt, new(defaultKey: "Alt")},
            { KeyCode.VcLeftControl, new(defaultKey: "Ctrl")},
            { KeyCode.VcEscape, new(defaultKey: "Esc")},
            
            // NumPad
            { KeyCode.VcNumPad0, new(defaultKey: "0") },
            { KeyCode.VcNumPad1, new(defaultKey: "1") },
            { KeyCode.VcNumPad2, new(defaultKey: "2") },
            { KeyCode.VcNumPad3, new(defaultKey: "3") },
            { KeyCode.VcNumPad4, new(defaultKey: "4") },
            { KeyCode.VcNumPad5, new(defaultKey: "5") },
            { KeyCode.VcNumPad6, new(defaultKey: "6") },
            { KeyCode.VcNumPad7, new(defaultKey: "7") },
            { KeyCode.VcNumPad8, new(defaultKey: "8") },
            { KeyCode.VcNumPad9, new(defaultKey: "9") },

            // Arrow
            { KeyCode.VcLeft, new(defaultKey: "◀") },
            { KeyCode.VcRight, new(defaultKey: "▶") },
        };
    }

    #endregion
}

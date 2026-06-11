using SharpHook.Data;
using System.Windows;
using System.Windows.Controls;
using Dreamine.UI.Abstractions.DreamineVirtualKeyboard;
using Dreamine.UI.Abstractions.DreamineVirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public class Key : Button
{
    #region Variable

    private static Dictionary<KeyCode, KeyData> _dicKeyData = new();

    #endregion

    #region Dependency Property

    #region IsPressed

    public static readonly new DependencyProperty IsPressedProperty =
        DependencyProperty.Register(nameof(IsPressed),
            typeof(bool),
            typeof(Key));

    public new bool IsPressed
    {
        get { return (bool)GetValue(IsPressedProperty); }
        set { SetValue(IsPressedProperty, value); }
    }

    #endregion

    #region KeyCode

    public static readonly DependencyProperty KeyCodeProperty =
        DependencyProperty.Register(nameof(KeyCode), typeof(KeyCode), typeof(Key));

    public KeyCode KeyCode
    {
        get { return (KeyCode)GetValue(KeyCodeProperty); }
        set { SetValue(KeyCodeProperty, value); }
    }

    #endregion Public Method

    public void UpdateKey(bool shift, bool capsLock, eLanguageCode languageCode, bool imeMode)
    {
        var keyData = _dicKeyData[KeyCode];

        var (displayKey, displayShiftKey) = GetKeyData(imeMode ? languageCode : eLanguageCode.en_US);

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

        Content = displayKey;
    }

    #endregion

    #region Constructor

    static Key()
    {
        MappingKeys();
    }

    public Key()
    {
        Focusable = false;
        IsTabStop = false;
        ClickMode = ClickMode.Press;
    }

    #endregion


    #region Private Method
    private (string, string) GetKeyData(eLanguageCode language)
    {
        var keyData = _dicKeyData[KeyCode];

        var key = language switch
        {
            eLanguageCode.en_US => keyData.DefaultKey,
            eLanguageCode.ko_KR => keyData.KorKey,
            eLanguageCode.zh_CN => keyData.ChnKey,
            _ => keyData.DefaultKey
        };

        var shiftKey = language switch
        {
            eLanguageCode.en_US => keyData.ShiftKey,
            eLanguageCode.ko_KR => keyData.KorShiftKey,
            eLanguageCode.zh_CN => keyData.ChnShiftKey,
            _ => keyData.ShiftKey
        };

        if (string.IsNullOrEmpty(key))
        {
            key = keyData.DefaultKey;
        }

        return (key, shiftKey);
    }

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
            { KeyCode.VcY, new(defaultKey: "Y", korKey: "ㅛ", chnKey: "卜") },
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

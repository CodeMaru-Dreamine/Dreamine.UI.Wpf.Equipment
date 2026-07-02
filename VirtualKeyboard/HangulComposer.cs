namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

internal sealed class HangulComposer
{
    private static readonly string[] Cho =
    [
        "ㄱ", "ㄲ", "ㄴ", "ㄷ", "ㄸ", "ㄹ", "ㅁ", "ㅂ", "ㅃ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅉ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    private static readonly string[] Jung =
    [
        "ㅏ", "ㅐ", "ㅑ", "ㅒ", "ㅓ", "ㅔ", "ㅕ", "ㅖ", "ㅗ", "ㅘ",
        "ㅙ", "ㅚ", "ㅛ", "ㅜ", "ㅝ", "ㅞ", "ㅟ", "ㅠ", "ㅡ", "ㅢ", "ㅣ"
    ];

    private static readonly string[] Jong =
    [
        "", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ",
        "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    private static readonly Dictionary<string, int> ChoIndex = Cho
        .Select((value, index) => (value, index))
        .ToDictionary(x => x.value, x => x.index);

    private static readonly Dictionary<string, int> JungIndex = Jung
        .Select((value, index) => (value, index))
        .ToDictionary(x => x.value, x => x.index);

    private static readonly Dictionary<string, int> JongIndex = Jong
        .Select((value, index) => (value, index))
        .Where(x => x.value.Length > 0)
        .ToDictionary(x => x.value, x => x.index);

    private static readonly Dictionary<(int First, int Second), int> CombinedJung = new()
    {
        [(8, 0)] = 9,
        [(8, 1)] = 10,
        [(8, 20)] = 11,
        [(13, 4)] = 14,
        [(13, 5)] = 15,
        [(13, 20)] = 16,
        [(18, 20)] = 19,
    };

    private static readonly Dictionary<(int First, int Second), int> CombinedJong = new()
    {
        [(1, 19)] = 3,
        [(4, 22)] = 5,
        [(4, 27)] = 6,
        [(8, 1)] = 9,
        [(8, 16)] = 10,
        [(8, 17)] = 11,
        [(8, 19)] = 12,
        [(8, 25)] = 13,
        [(8, 26)] = 14,
        [(8, 27)] = 15,
        [(17, 19)] = 18,
    };

    private static readonly Dictionary<int, (int First, int Second)> SplitJong = CombinedJong
        .ToDictionary(x => x.Value, x => x.Key);

    private int _cho = -1;
    private int _jung = -1;
    private int _jong;

    public void Reset()
    {
        _cho = -1;
        _jung = -1;
        _jong = 0;
    }

    public static bool IsComposableJamo(string text)
    {
        return text.Length == 1 && (ChoIndex.ContainsKey(text) || JungIndex.ContainsKey(text));
    }

    public HangulEdit Input(string text)
    {
        return Input(text, string.Empty);
    }

    public HangulEdit Input(string text, string textBeforeCaret)
    {
        if (string.IsNullOrEmpty(text) || text.Length != 1)
        {
            Reset();
            return new HangulEdit(0, text);
        }

        if (ChoIndex.TryGetValue(text, out var cho))
        {
            if (TryComposeConsonantWithTrailingText(textBeforeCaret, text, out var edit))
                return edit;

            if (!string.IsNullOrEmpty(textBeforeCaret))
            {
                Reset();
                return new HangulEdit(0, text);
            }

            return InputConsonant(text, cho);
        }

        if (JungIndex.TryGetValue(text, out var jung))
        {
            if (TryComposeVowelWithTrailingText(textBeforeCaret, jung, out var edit))
                return edit;

            if (!string.IsNullOrEmpty(textBeforeCaret))
            {
                Reset();
                return new HangulEdit(0, text);
            }

            return InputVowel(text, jung);
        }

        Reset();
        return new HangulEdit(0, text);
    }

    private bool TryComposeConsonantWithTrailingText(string textBeforeCaret, string text, out HangulEdit edit)
    {
        edit = default;
        if (string.IsNullOrEmpty(textBeforeCaret) || !JongIndex.TryGetValue(text, out var jong))
            return false;

        var last = textBeforeCaret[^1];
        if (!TryDecompose(last, out var cho, out var jung, out var currentJong))
            return false;

        if (currentJong == 0)
        {
            _cho = cho;
            _jung = jung;
            _jong = jong;
            edit = new HangulEdit(1, Compose(_cho, _jung, _jong));
            return true;
        }

        if (CombinedJong.TryGetValue((currentJong, jong), out var combinedJong))
        {
            _cho = cho;
            _jung = jung;
            _jong = combinedJong;
            edit = new HangulEdit(1, Compose(_cho, _jung, _jong));
            return true;
        }

        return false;
    }

    private bool TryComposeVowelWithTrailingText(string textBeforeCaret, int jung, out HangulEdit edit)
    {
        edit = default;
        if (string.IsNullOrEmpty(textBeforeCaret))
            return false;

        var last = textBeforeCaret[^1].ToString();
        if (ChoIndex.TryGetValue(last, out var trailingCho))
        {
            _cho = trailingCho;
            _jung = jung;
            _jong = 0;
            edit = new HangulEdit(1, Compose(_cho, _jung, 0));
            return true;
        }

        var lastChar = textBeforeCaret[^1];
        if (!TryDecompose(lastChar, out var cho, out var currentJung, out var jong) || jong == 0)
            return false;

        if (SplitJong.TryGetValue(jong, out var split))
        {
            var previous = Compose(cho, currentJung, split.First);
            _cho = ToChoIndex(Jong[split.Second]);
            _jung = jung;
            _jong = 0;
            edit = new HangulEdit(1, previous + Compose(_cho, _jung, 0));
            return true;
        }

        _cho = ToChoIndex(Jong[jong]);
        _jung = jung;
        _jong = 0;
        edit = new HangulEdit(1, Compose(cho, currentJung, 0) + Compose(_cho, _jung, 0));
        return true;
    }

    private HangulEdit InputConsonant(string text, int cho)
    {
        if (_cho < 0)
        {
            _cho = cho;
            return new HangulEdit(0, text);
        }

        if (_jung < 0)
        {
            _cho = cho;
            return new HangulEdit(0, text);
        }

        if (_jong == 0)
        {
            if (JongIndex.TryGetValue(text, out var jong))
            {
                _jong = jong;
                return new HangulEdit(1, Compose(_cho, _jung, _jong));
            }

            _cho = cho;
            _jung = -1;
            _jong = 0;
            return new HangulEdit(0, text);
        }

        if (JongIndex.TryGetValue(text, out var nextJong) &&
            CombinedJong.TryGetValue((_jong, nextJong), out var combinedJong))
        {
            _jong = combinedJong;
            return new HangulEdit(1, Compose(_cho, _jung, _jong));
        }

        _cho = cho;
        _jung = -1;
        _jong = 0;
        return new HangulEdit(0, text);
    }

    private HangulEdit InputVowel(string text, int jung)
    {
        if (_cho < 0)
        {
            Reset();
            return new HangulEdit(0, text);
        }

        if (_jung < 0)
        {
            _jung = jung;
            return new HangulEdit(1, Compose(_cho, _jung, 0));
        }

        if (_jong == 0)
        {
            if (CombinedJung.TryGetValue((_jung, jung), out var combinedJung))
            {
                _jung = combinedJung;
                return new HangulEdit(1, Compose(_cho, _jung, 0));
            }

            Reset();
            return new HangulEdit(0, text);
        }

        var previousCho = _cho;
        var previousJung = _jung;
        var previousJong = _jong;

        if (SplitJong.TryGetValue(previousJong, out var split))
        {
            var previous = Compose(previousCho, previousJung, split.First);
            _cho = ToChoIndex(Jong[split.Second]);
            _jung = jung;
            _jong = 0;
            return new HangulEdit(1, previous + Compose(_cho, _jung, 0));
        }

        _cho = ToChoIndex(Jong[previousJong]);
        _jung = jung;
        _jong = 0;
        return new HangulEdit(1, Compose(previousCho, previousJung, 0) + Compose(_cho, _jung, 0));
    }

    private static int ToChoIndex(string jong)
    {
        return ChoIndex.TryGetValue(jong, out var cho) ? cho : ChoIndex["ㅇ"];
    }

    private static string Compose(int cho, int jung, int jong)
    {
        return char.ConvertFromUtf32(0xAC00 + ((cho * 21) + jung) * 28 + jong);
    }

    private static bool TryDecompose(char value, out int cho, out int jung, out int jong)
    {
        var code = value - 0xAC00;
        if (code < 0 || code >= 11172)
        {
            cho = -1;
            jung = -1;
            jong = 0;
            return false;
        }

        cho = code / (21 * 28);
        jung = (code % (21 * 28)) / 28;
        jong = code % 28;
        return true;
    }
}

internal readonly record struct HangulEdit(int ReplaceCount, string Text);

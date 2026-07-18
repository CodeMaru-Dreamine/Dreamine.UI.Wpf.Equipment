namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>두벌식 자모 입력을 완성형 한글 음절과 편집 지시로 조합합니다.</para>
/// \endif
/// \if EN
/// <para>Composes two-set Korean jamo input into precomposed Hangul syllables and edit instructions.</para>
/// \endif
/// </summary>
internal sealed class HangulComposer
{
    /// <summary>
    /// \if KO
    /// <para>초성 인덱스 순서의 자음 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores consonants in leading-jamo index order.</para>
    /// \endif
    /// </summary>
    private static readonly string[] Cho =
    [
        "ㄱ", "ㄲ", "ㄴ", "ㄷ", "ㄸ", "ㄹ", "ㅁ", "ㅂ", "ㅃ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅉ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    /// <summary>
    /// \if KO
    /// <para>중성 인덱스 순서의 모음 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores vowels in medial-jamo index order.</para>
    /// \endif
    /// </summary>
    private static readonly string[] Jung =
    [
        "ㅏ", "ㅐ", "ㅑ", "ㅒ", "ㅓ", "ㅔ", "ㅕ", "ㅖ", "ㅗ", "ㅘ",
        "ㅙ", "ㅚ", "ㅛ", "ㅜ", "ㅝ", "ㅞ", "ㅟ", "ㅠ", "ㅡ", "ㅢ", "ㅣ"
    ];

    /// <summary>
    /// \if KO
    /// <para>종성 인덱스 순서의 받침 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores trailing consonants in final-jamo index order.</para>
    /// \endif
    /// </summary>
    private static readonly string[] Jong =
    [
        "", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ",
        "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ",
        "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
    ];

    /// <summary>
    /// \if KO
    /// <para>초성 문자열에서 유니코드 초성 인덱스로의 매핑입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps leading-consonant strings to Unicode leading-jamo indices.</para>
    /// \endif
    /// </summary>
    private static readonly Dictionary<string, int> ChoIndex = Cho
        .Select((value, index) => (value, index))
        .ToDictionary(x => x.value, x => x.index);

    /// <summary>
    /// \if KO
    /// <para>중성 문자열에서 유니코드 중성 인덱스로의 매핑입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps vowel strings to Unicode medial-jamo indices.</para>
    /// \endif
    /// </summary>
    private static readonly Dictionary<string, int> JungIndex = Jung
        .Select((value, index) => (value, index))
        .ToDictionary(x => x.value, x => x.index);

    /// <summary>
    /// \if KO
    /// <para>종성 문자열에서 유니코드 종성 인덱스로의 매핑입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps trailing-consonant strings to Unicode final-jamo indices.</para>
    /// \endif
    /// </summary>
    private static readonly Dictionary<string, int> JongIndex = Jong
        .Select((value, index) => (value, index))
        .Where(x => x.value.Length > 0)
        .ToDictionary(x => x.value, x => x.index);

    /// <summary>
    /// \if KO
    /// <para>기본 모음 두 개를 복합 모음 인덱스로 결합하는 표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps pairs of basic vowels to combined-vowel indices.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>종성 두 개를 복합 종성 인덱스로 결합하는 표입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps pairs of trailing consonants to combined-final indices.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>복합 종성을 원래 두 종성으로 분리하는 역매핑입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Maps combined finals back to their two component finals.</para>
    /// \endif
    /// </summary>
    private static readonly Dictionary<int, (int First, int Second)> SplitJong = CombinedJong
        .ToDictionary(x => x.Value, x => x.Key);

    /// <summary>
    /// \if KO
    /// <para>현재 조합 중인 초성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the leading-jamo index currently being composed.</para>
    /// \endif
    /// </summary>
    private int _cho = -1;
    /// <summary>
    /// \if KO
    /// <para>현재 조합 중인 중성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the medial-jamo index currently being composed.</para>
    /// \endif
    /// </summary>
    private int _jung = -1;
    /// <summary>
    /// \if KO
    /// <para>현재 조합 중인 종성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the final-jamo index currently being composed.</para>
    /// \endif
    /// </summary>
    private int _jong;

    /// <summary>
    /// \if KO
    /// <para>진행 중인 한글 음절 조합 상태를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Resets the in-progress Hangul-syllable composition state.</para>
    /// \endif
    /// </summary>
    public void Reset()
    {
        _cho = -1;
        _jung = -1;
        _jong = 0;
    }

    /// <summary>
    /// \if KO
    /// <para>문자열이 단일 조합 가능 초성 또는 중성 자모인지 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether a string is a single composable leading consonant or vowel.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>검사할 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The string to inspect.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>조합 가능한 단일 자모이면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> for a single composable jamo.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NullReferenceException">
    /// \if KO
    /// <para><paramref name="text"/>가 <see langword="null"/>일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="text"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public static bool IsComposableJamo(string text)
    {
        return text.Length == 1 && (ChoIndex.ContainsKey(text) || JungIndex.ContainsKey(text));
    }

    /// <summary>
    /// \if KO
    /// <para>앞 문맥 없이 한 자모를 현재 조합 상태에 입력합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Inputs one jamo into the current composition state without preceding context.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>입력할 문자 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The character string to input.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>교체할 문자 수와 삽입할 텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The number of characters to replace and the text to insert.</para>
    /// \endif
    /// </returns>
    public HangulEdit Input(string text)
    {
        return Input(text, string.Empty);
    }

    /// <summary>
    /// \if KO
    /// <para>캐럿 앞 텍스트를 고려하여 한 자모를 입력하고 필요한 음절 교체 지시를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Inputs one jamo using text before the caret and returns the required syllable-replacement instruction.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>입력할 문자 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The character string to input.</para>
    /// \endif
    /// </param>
    /// <param name="textBeforeCaret">
    /// \if KO
    /// <para>캐럿 앞의 기존 텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Existing text before the caret.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>교체할 문자 수와 새 조합 텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The replacement count and newly composed text.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>캐럿 앞 완성 음절에 새 자음을 단일 또는 복합 종성으로 결합합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Tries to combine a new consonant with the preceding syllable as a single or compound final.</para>
    /// \endif
    /// </summary>
    /// <param name="textBeforeCaret">
    /// \if KO
    /// <para>캐럿 앞 텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Text before the caret.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>새 자음입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The new consonant.</para>
    /// \endif
    /// </param>
    /// <param name="edit">
    /// \if KO
    /// <para>성공 시 앞 음절을 교체할 편집 지시입니다.</para>
    /// \endif
    /// \if EN
    /// <para>On success, the edit instruction replacing the preceding syllable.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>결합되었으면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> if composition succeeded.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>캐럿 앞 자모 또는 받침 있는 음절에 모음을 결합하고 받침을 다음 초성으로 이동합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Combines a vowel with a preceding jamo or syllable, moving any final consonant to the next leading position.</para>
    /// \endif
    /// </summary>
    /// <param name="textBeforeCaret">
    /// \if KO
    /// <para>캐럿 앞 텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Text before the caret.</para>
    /// \endif
    /// </param>
    /// <param name="jung">
    /// \if KO
    /// <para>새 중성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The new medial-jamo index.</para>
    /// \endif
    /// </param>
    /// <param name="edit">
    /// \if KO
    /// <para>성공 시 적용할 편집 지시입니다.</para>
    /// \endif
    /// \if EN
    /// <para>On success, the edit instruction to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>결합되었으면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> if composition succeeded.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>현재 조합 상태에 자음을 초성 또는 종성으로 적용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Applies a consonant to the current state as a leading or final jamo.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>입력 자음 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input consonant string.</para>
    /// \endif
    /// </param>
    /// <param name="cho">
    /// \if KO
    /// <para>입력 자음의 초성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The leading-jamo index of the input consonant.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>적용할 편집 지시입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The edit instruction to apply.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>현재 조합 상태에 모음을 중성 또는 복합 중성으로 적용하고 필요하면 종성을 분리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Applies a vowel as a medial or compound medial and splits a final consonant when required.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>입력 모음 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input vowel string.</para>
    /// \endif
    /// </param>
    /// <param name="jung">
    /// \if KO
    /// <para>입력 모음의 중성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The medial-jamo index of the input vowel.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>적용할 편집 지시입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The edit instruction to apply.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>종성 자모를 같은 모양의 초성 인덱스로 변환하며 없으면 이응을 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Converts a final-jamo string to the matching leading index, falling back to ieung.</para>
    /// \endif
    /// </summary>
    /// <param name="jong">
    /// \if KO
    /// <para>변환할 종성 자모입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The final jamo to convert.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>대응 초성 또는 이응 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The matching leading-jamo or ieung index.</para>
    /// \endif
    /// </returns>
    private static int ToChoIndex(string jong)
    {
        return ChoIndex.TryGetValue(jong, out var cho) ? cho : ChoIndex["ㅇ"];
    }

    /// <summary>
    /// \if KO
    /// <para>초성·중성·종성 인덱스를 완성형 한글 음절로 조합합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Composes leading, medial, and final indices into a precomposed Hangul syllable.</para>
    /// \endif
    /// </summary>
    /// <param name="cho">
    /// \if KO
    /// <para>초성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The leading-jamo index.</para>
    /// \endif
    /// </param>
    /// <param name="jung">
    /// \if KO
    /// <para>중성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The medial-jamo index.</para>
    /// \endif
    /// </param>
    /// <param name="jong">
    /// \if KO
    /// <para>종성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The final-jamo index.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>조합된 한글 음절 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The composed Hangul-syllable string.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>계산한 코드 포인트가 유효한 유니코드 범위를 벗어날 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the computed code point is outside the valid Unicode range.</para>
    /// \endif
    /// </exception>
    private static string Compose(int cho, int jung, int jong)
    {
        return char.ConvertFromUtf32(0xAC00 + ((cho * 21) + jung) * 28 + jong);
    }

    /// <summary>
    /// \if KO
    /// <para>완성형 한글 음절을 초성·중성·종성 인덱스로 분해합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Decomposes a precomposed Hangul syllable into leading, medial, and final indices.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>분해할 문자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The character to decompose.</para>
    /// \endif
    /// </param>
    /// <param name="cho">
    /// \if KO
    /// <para>성공 시 초성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>On success, the leading-jamo index.</para>
    /// \endif
    /// </param>
    /// <param name="jung">
    /// \if KO
    /// <para>성공 시 중성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>On success, the medial-jamo index.</para>
    /// \endif
    /// </param>
    /// <param name="jong">
    /// \if KO
    /// <para>성공 시 종성 인덱스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>On success, the final-jamo index.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>완성형 한글이면 <see langword="true"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> for a precomposed Hangul syllable.</para>
    /// \endif
    /// </returns>
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

/// <summary>
/// \if KO
/// <para>캐럿 앞에서 교체할 문자 수와 삽입할 조합 텍스트를 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Represents the number of characters to replace before the caret and the composed text to insert.</para>
/// \endif
/// </summary>
/// <param name="ReplaceCount">
/// \if KO
/// <para>캐럿 앞에서 제거할 문자 수입니다.</para>
/// \endif
/// \if EN
/// <para>The number of characters to remove before the caret.</para>
/// \endif
/// </param>
/// <param name="Text">
/// \if KO
/// <para>삽입할 조합 결과입니다.</para>
/// \endif
/// \if EN
/// <para>The composed result to insert.</para>
/// \endif
/// </param>
internal readonly record struct HangulEdit(int ReplaceCount, string Text);

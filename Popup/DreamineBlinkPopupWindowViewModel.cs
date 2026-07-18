using System.Windows.Input;
using System.Windows.Media;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.UI.Wpf.Equipment.Popup;

using Dreamine.UI.Abstractions.Popup;

/// <summary>
/// \if KO
/// <para>점멸 팝업 창의 콘텐츠, 동작, 색상, 타이밍 및 표시 상태를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides content, actions, colors, timing, and display state for a blinking popup window.</para>
/// \endif
/// </summary>
public sealed class DreamineBlinkPopupWindowViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>제목 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title.</para>
    /// \endif
    /// </summary>
    private string? _title;
    /// <summary>
    /// \if KO
    /// <para>팝업 제목을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the popup title.</para>
    /// \endif
    /// </summary>
    public string? Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>
    /// \if KO
    /// <para>메시지 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the message.</para>
    /// \endif
    /// </summary>
    private string? _message;
    /// <summary>
    /// \if KO
    /// <para>팝업 메시지를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the popup message.</para>
    /// \endif
    /// </summary>
    public string? Message { get => _message; set => SetProperty(ref _message, value); }

    /// <summary>
    /// \if KO
    /// <para>사용자 콘텐츠 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores custom content.</para>
    /// \endif
    /// </summary>
    private object? _content;
    /// <summary>
    /// \if KO
    /// <para>팝업에 표시할 사용자 콘텐츠를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets custom content displayed by the popup.</para>
    /// \endif
    /// </summary>
    public object? Content { get => _content; set => SetProperty(ref _content, value); }

    /// <summary>
    /// \if KO
    /// <para>확인 버튼 텍스트 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the OK-button text.</para>
    /// \endif
    /// </summary>
    private string? _okText;
    /// <summary>
    /// \if KO
    /// <para>확인 버튼 텍스트를 가져오거나 설정하고 표시 및 실행 가능 상태를 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets OK-button text and updates visibility and command availability.</para>
    /// \endif
    /// </summary>
    public string? OkText
    {
        get => _okText;
        set
        {
            if (SetProperty(ref _okText, value))
            {
                OnPropertyChanged(nameof(OkVisible));
                ((RelayCommand)OkCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>취소 버튼 텍스트 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cancel-button text.</para>
    /// \endif
    /// </summary>
    private string? _cancelText;
    /// <summary>
    /// \if KO
    /// <para>취소 버튼 텍스트를 가져오거나 설정하고 표시 및 실행 가능 상태를 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets cancel-button text and updates visibility and command availability.</para>
    /// \endif
    /// </summary>
    public string? CancelText
    {
        get => _cancelText;
        set
        {
            if (SetProperty(ref _cancelText, value))
            {
                OnPropertyChanged(nameof(CancelVisible));
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>팝업이 모달로 표시되는지 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the popup is displayed modally.</para>
    /// \endif
    /// </summary>
    public bool IsModal { get; }

    /// <summary>
    /// \if KO
    /// <para>최상위 표시 상태 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the topmost state.</para>
    /// \endif
    /// </summary>
    private bool _topMost;
    /// <summary>
    /// \if KO
    /// <para>창을 최상위로 유지할지 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the window remains topmost.</para>
    /// \endif
    /// </summary>
    public bool TopMost { get => _topMost; set => SetProperty(ref _topMost, value); }

    /// <summary>
    /// \if KO
    /// <para>전체 화면 상태 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the fullscreen state.</para>
    /// \endif
    /// </summary>
    private bool _fullscreen;
    /// <summary>
    /// \if KO
    /// <para>팝업을 전체 화면으로 표시할지 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the popup is shown fullscreen.</para>
    /// \endif
    /// </summary>
    public bool Fullscreen { get => _fullscreen; set => SetProperty(ref _fullscreen, value); }

    /// <summary>
    /// \if KO
    /// <para>점멸 사용 상태 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the blinking-enabled state.</para>
    /// \endif
    /// </summary>
    private bool _useBlink;
    /// <summary>
    /// \if KO
    /// <para>배경 점멸 효과 사용 여부를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the background blink effect is enabled.</para>
    /// \endif
    /// </summary>
    public bool UseBlink { get => _useBlink; set => SetProperty(ref _useBlink, value); }

    /// <summary>
    /// \if KO
    /// <para>콘텐츠 카드 사용 상태 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the content-card state.</para>
    /// \endif
    /// </summary>
    private bool _useContentCard;
    /// <summary>
    /// \if KO
    /// <para>콘텐츠 카드 표시 여부를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets whether the content card is displayed.</para>
    /// \endif
    /// </summary>
    public bool UseContentCard { get => _useContentCard; set => SetProperty(ref _useContentCard, value); }

    /// <summary>
    /// \if KO
    /// <para>첫 번째 루트 색상 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the first root color.</para>
    /// \endif
    /// </summary>
    private Color _rootColor1;
    /// <summary>
    /// \if KO
    /// <para>점멸의 첫 번째 루트 색상을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the first root color used by blinking.</para>
    /// \endif
    /// </summary>
    public Color RootColor1 { get => _rootColor1; set => SetProperty(ref _rootColor1, value); }

    /// <summary>
    /// \if KO
    /// <para>두 번째 루트 색상 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the second root color.</para>
    /// \endif
    /// </summary>
    private Color _rootColor2;
    /// <summary>
    /// \if KO
    /// <para>점멸의 두 번째 루트 색상을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the second root color used by blinking.</para>
    /// \endif
    /// </summary>
    public Color RootColor2 { get => _rootColor2; set => SetProperty(ref _rootColor2, value); }

    /// <summary>
    /// \if KO
    /// <para>전경색 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the foreground color.</para>
    /// \endif
    /// </summary>
    private Color _foregroundColor;
    /// <summary>
    /// \if KO
    /// <para>제목과 메시지 전경색을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title and message foreground color.</para>
    /// \endif
    /// </summary>
    public Color ForegroundColor { get => _foregroundColor; set => SetProperty(ref _foregroundColor, value); }

    /// <summary>
    /// \if KO
    /// <para>첫 번째 불투명도 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the first opacity.</para>
    /// \endif
    /// </summary>
    private double _opacity1;
    /// <summary>
    /// \if KO
    /// <para>첫 번째 점멸 상태 불투명도를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the first blink-state opacity.</para>
    /// \endif
    /// </summary>
    public double Opacity1 { get => _opacity1; set => SetProperty(ref _opacity1, value); }

    /// <summary>
    /// \if KO
    /// <para>두 번째 불투명도 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the second opacity.</para>
    /// \endif
    /// </summary>
    private double _opacity2;
    /// <summary>
    /// \if KO
    /// <para>두 번째 점멸 상태 불투명도를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the second blink-state opacity.</para>
    /// \endif
    /// </summary>
    public double Opacity2 { get => _opacity2; set => SetProperty(ref _opacity2, value); }

    /// <summary>
    /// \if KO
    /// <para>점멸 간격 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the blink interval.</para>
    /// \endif
    /// </summary>
    private int _blinkIntervalMs;
    /// <summary>
    /// \if KO
    /// <para>점멸 간격을 밀리초로 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the blink interval in milliseconds.</para>
    /// \endif
    /// </summary>
    public int BlinkIntervalMs { get => _blinkIntervalMs; set => SetProperty(ref _blinkIntervalMs, value); }

    /// <summary>
    /// \if KO
    /// <para>점멸 반복 횟수 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the blink repetition count.</para>
    /// \endif
    /// </summary>
    private int _blinkRepeatCount;
    /// <summary>
    /// \if KO
    /// <para>점멸 반복 횟수를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the blink repetition count.</para>
    /// \endif
    /// </summary>
    public int BlinkRepeatCount { get => _blinkRepeatCount; set => SetProperty(ref _blinkRepeatCount, value); }

    /// <summary>
    /// \if KO
    /// <para>제목 글꼴 크기 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title font size.</para>
    /// \endif
    /// </summary>
    private double _titleFontSizeValue;
    /// <summary>
    /// \if KO
    /// <para>제목 글꼴 크기를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title font size.</para>
    /// \endif
    /// </summary>
    public double TitleFontSizeValue { get => _titleFontSizeValue; set => SetProperty(ref _titleFontSizeValue, value); }

    /// <summary>
    /// \if KO
    /// <para>메시지 글꼴 크기 저장 필드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the message font size.</para>
    /// \endif
    /// </summary>
    private double _messageFontSizeValue;
    /// <summary>
    /// \if KO
    /// <para>메시지 글꼴 크기를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message font size.</para>
    /// \endif
    /// </summary>
    public double MessageFontSizeValue { get => _messageFontSizeValue; set => SetProperty(ref _messageFontSizeValue, value); }

    /// <summary>
    /// \if KO
    /// <para>확인 버튼 텍스트가 있어 버튼을 표시할지 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether OK-button text exists and the button should be visible.</para>
    /// \endif
    /// </summary>
    public bool OkVisible     => !string.IsNullOrEmpty(OkText);
    /// <summary>
    /// \if KO
    /// <para>취소 버튼 텍스트가 있어 버튼을 표시할지 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether cancel-button text exists and the button should be visible.</para>
    /// \endif
    /// </summary>
    public bool CancelVisible => !string.IsNullOrEmpty(CancelText);

    /// <summary>
    /// \if KO
    /// <para>확인 또는 취소 결과와 함께 창 닫기가 요청될 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when window closure is requested with an OK or cancel result.</para>
    /// \endif
    /// </summary>
    public event EventHandler<bool?>? CloseRequested;

    /// <summary>
    /// \if KO
    /// <para>확인 결과로 닫기를 요청하는 명령을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the command that requests closure with an OK result.</para>
    /// \endif
    /// </summary>
    public ICommand OkCommand     { get; }
    /// <summary>
    /// \if KO
    /// <para>취소 결과로 닫기를 요청하는 명령을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the command that requests closure with a cancel result.</para>
    /// \endif
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>팝업 옵션에서 표시 상태를 초기화하고 확인·취소 명령을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes display state from popup options and configures OK and cancel commands.</para>
    /// \endif
    /// </summary>
    /// <param name="opt">
    /// \if KO
    /// <para>제목, 콘텐츠, 색상, 동작 및 타이밍을 제공하는 팝업 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Popup options providing title, content, colors, behavior, and timing.</para>
    /// \endif
    /// </param>
    /// <exception cref="NullReferenceException">
    /// \if KO
    /// <para><paramref name="opt"/>가 <see langword="null"/>일 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="opt"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public DreamineBlinkPopupWindowViewModel(BlinkPopupOptions opt)
    {
        _title              = opt.Title;
        _message            = opt.Message;
        _content            = opt.Content;
        _okText             = opt.OkText;
        _cancelText         = opt.CancelText;
        _rootColor1         = opt.Color1;
        _rootColor2         = opt.Color2;
        _foregroundColor    = opt.ForegroundColor;
        _opacity1           = opt.Opacity1;
        _opacity2           = opt.Opacity2;
        _blinkIntervalMs    = opt.BlinkIntervalMs;
        _blinkRepeatCount   = opt.BlinkRepeatCount;
        _titleFontSizeValue = opt.TitleFontSize;
        _messageFontSizeValue = opt.MessageFontSize;
        _useBlink           = opt.UseBlink;
        _fullscreen         = opt.Fullscreen;
        _topMost            = opt.TopMost;
        IsModal             = opt.IsModal;
        _useContentCard     = opt.UseContentCard;

        OkCommand     = new RelayCommand(() => CloseRequested?.Invoke(this, true),  () => !string.IsNullOrEmpty(OkText));
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, false), () => !string.IsNullOrEmpty(CancelText));
    }
}

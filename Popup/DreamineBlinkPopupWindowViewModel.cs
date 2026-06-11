using System.Windows.Input;
using System.Windows.Media;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.UI.Wpf.Equipment.Popup;

using Dreamine.UI.Abstractions.Popup;

/// <summary>BlinkPopupWindow용 ViewModel.</summary>
public sealed class DreamineBlinkPopupWindowViewModel : ViewModelBase
{
    private string? _title;
    public string? Title { get => _title; set => SetProperty(ref _title, value); }

    private string? _message;
    public string? Message { get => _message; set => SetProperty(ref _message, value); }

    private object? _content;
    public object? Content { get => _content; set => SetProperty(ref _content, value); }

    private string? _okText;
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

    private string? _cancelText;
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

    public bool IsModal { get; }

    private bool _topMost;
    public bool TopMost { get => _topMost; set => SetProperty(ref _topMost, value); }

    private bool _fullscreen;
    public bool Fullscreen { get => _fullscreen; set => SetProperty(ref _fullscreen, value); }

    private bool _useBlink;
    public bool UseBlink { get => _useBlink; set => SetProperty(ref _useBlink, value); }

    private bool _useContentCard;
    public bool UseContentCard { get => _useContentCard; set => SetProperty(ref _useContentCard, value); }

    private Color _rootColor1;
    public Color RootColor1 { get => _rootColor1; set => SetProperty(ref _rootColor1, value); }

    private Color _rootColor2;
    public Color RootColor2 { get => _rootColor2; set => SetProperty(ref _rootColor2, value); }

    private Color _foregroundColor;
    public Color ForegroundColor { get => _foregroundColor; set => SetProperty(ref _foregroundColor, value); }

    private double _opacity1;
    public double Opacity1 { get => _opacity1; set => SetProperty(ref _opacity1, value); }

    private double _opacity2;
    public double Opacity2 { get => _opacity2; set => SetProperty(ref _opacity2, value); }

    private int _blinkIntervalMs;
    public int BlinkIntervalMs { get => _blinkIntervalMs; set => SetProperty(ref _blinkIntervalMs, value); }

    private int _blinkRepeatCount;
    public int BlinkRepeatCount { get => _blinkRepeatCount; set => SetProperty(ref _blinkRepeatCount, value); }

    private double _titleFontSizeValue;
    public double TitleFontSizeValue { get => _titleFontSizeValue; set => SetProperty(ref _titleFontSizeValue, value); }

    private double _messageFontSizeValue;
    public double MessageFontSizeValue { get => _messageFontSizeValue; set => SetProperty(ref _messageFontSizeValue, value); }

    public bool OkVisible     => !string.IsNullOrEmpty(OkText);
    public bool CancelVisible => !string.IsNullOrEmpty(CancelText);

    public event EventHandler<bool?>? CloseRequested;

    public ICommand OkCommand     { get; }
    public ICommand CancelCommand { get; }

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

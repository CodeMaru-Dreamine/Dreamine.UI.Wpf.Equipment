using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using System.Windows.Media;
using VsLibrary.Common.MVVM.ViewModels;

namespace Dreamine.UI.Wpf.Equipment.Popup
{
	/// <summary>\brief BlinkPopupWindow용 ViewModel(툴킷 Source Generator 기반).</summary>
	public sealed partial class DreamineBlinkPopupWindowViewModel : ViewModelBase
	{
		[ObservableProperty] private string? _title;
		[ObservableProperty] private string? _message;
		[ObservableProperty] private object? _content;
		[ObservableProperty] private string? _okText;
		[ObservableProperty] private string? _cancelText;

		/// <summary>\brief 모달 여부.</summary>
		public bool IsModal { get; }

		[ObservableProperty] private bool _topMost;
		[ObservableProperty] private bool _fullscreen;
		[ObservableProperty] private bool _useBlink;

		/// <summary>\brief 중앙 카드 사용 여부.</summary>
		[ObservableProperty] private bool _useContentCard;

		[ObservableProperty] private Color _rootColor1;
		[ObservableProperty] private Color _rootColor2;
		[ObservableProperty] private Color _foregroundColor;
		[ObservableProperty] private double _opacity1;
		[ObservableProperty] private double _opacity2;
		[ObservableProperty] private int _blinkIntervalMs;
		[ObservableProperty] private int _blinkRepeatCount;

		[ObservableProperty] private double _titleFontSizeValue;
		[ObservableProperty] private double _messageFontSizeValue;

		/// <summary>\brief 닫힘 요청(코드비하인드에서 수신).</summary>
		public event EventHandler<bool?>? CloseRequested;

		/// <summary>\brief OK 버튼 표시 가능 여부(null/빈문자면 숨김).</summary>
		public bool OkVisible => !string.IsNullOrEmpty(OkText);

		/// <summary>\brief Cancel 버튼 표시 가능 여부(null/빈문자면 숨김).</summary>
		public bool CancelVisible => !string.IsNullOrEmpty(CancelText);

		/// <summary>\brief 옵션 DTO로부터 초기화.</summary>
		public DreamineBlinkPopupWindowViewModel(BlinkPopupOptions opt)
		{
			_title = opt.Title;
			_message = opt.Message;
			_content = opt.Content;

			_okText = opt.OkText;
			_cancelText = opt.CancelText;

			_rootColor1 = opt.Color1;
			_rootColor2 = opt.Color2;
			_foregroundColor = opt.ForegroundColor;
			_opacity1 = opt.Opacity1;
			_opacity2 = opt.Opacity2;
			_blinkIntervalMs = opt.BlinkIntervalMs;
			_blinkRepeatCount = opt.BlinkRepeatCount;

			_titleFontSizeValue = opt.TitleFontSize;
			_messageFontSizeValue = opt.MessageFontSize;

			_useBlink = opt.UseBlink;
			_fullscreen = opt.Fullscreen;
			_topMost = opt.TopMost;
			IsModal = opt.IsModal;

			_useContentCard = opt.UseContentCard;
		}

		[RelayCommand(CanExecute = nameof(CanOk))]
		private void Ok() => CloseRequested?.Invoke(this, true);

		[RelayCommand(CanExecute = nameof(CanCancel))]
		private void Cancel() => CloseRequested?.Invoke(this, false);

		private bool CanOk() => !string.IsNullOrEmpty(OkText);
		private bool CanCancel() => !string.IsNullOrEmpty(CancelText);

		partial void OnOkTextChanged(string? value)
		{
			OnPropertyChanged(nameof(OkVisible));
			OkCommand.NotifyCanExecuteChanged();
		}

		partial void OnCancelTextChanged(string? value)
		{
			OnPropertyChanged(nameof(CancelVisible));
			CancelCommand.NotifyCanExecuteChanged();
		}
	}
}

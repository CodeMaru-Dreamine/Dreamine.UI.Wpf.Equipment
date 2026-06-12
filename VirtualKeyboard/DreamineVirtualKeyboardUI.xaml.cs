using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Dreamine.UI.Wpf.Controls;
using Dreamine.UI.Abstractions.VirtualKeyboard;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public partial class DreamineVirtualKeyboardUI : DreamineVirtualKeyboard
{
	private readonly DispatcherTimer _debounceTimer;

	public bool InternalUpdate { get; set; } = false;

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public decimal Minimum
	{
		get => (decimal)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public decimal Maximum
	{
		get => (decimal)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public string DecimalFormat { get; set; } = string.Empty;

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value), typeof(string), typeof(DreamineVirtualKeyboardUI),
		new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum), typeof(decimal), typeof(DreamineVirtualKeyboardUI),
		new PropertyMetadata(decimal.MinValue));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum), typeof(decimal), typeof(DreamineVirtualKeyboardUI),
		new PropertyMetadata(decimal.MaxValue));

	public DreamineVirtualKeyboardUI()
	{
		InitializeComponent();

		_debounceTimer = new DispatcherTimer(DispatcherPriority.SystemIdle, VkbTextBox.Dispatcher)
		{
			Interval = TimeSpan.FromSeconds(1),
		};

		OnKeyboardLanguageChanged += OnKeyboardLanguageChangedHandler;
		RegisterVkbInputEvents();
		SetInputMethod();
	}

	private void RegisterVkbInputEvents()
	{
		VkbTextBox.Loaded += VkbInput_Loaded;
		VkbTextBox.Unloaded += VkbInput_Unloaded;
		VkbTextBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
		VkbPasswordBox.Loaded += VkbInput_Loaded;
		VkbPasswordBox.Unloaded += VkbInput_Unloaded;
		VkbPasswordBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
	}

	private void VkbInput_Loaded(object sender, RoutedEventArgs e)
	{
		FocusVkbTextBox();
		FocusVkbPasswordBox();
		InternalUpdate = true;
		if (Layout != eVkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || IsOuterRange()))
		{
			ClampNumericValue(true);
		}
	}

	private void VkbInput_Unloaded(object sender, RoutedEventArgs e)
	{
		InternalUpdate = true;
		_debounceTimer.Stop();
		_debounceTimer.Tick -= NormalizeNumericInput;
	}

	private void VkbInput_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		FocusVkbTextBox();
		FocusVkbPasswordBox();

		if (Layout != eVkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || IsOuterRange()))
		{
			ClampNumericValue(true);
		}

		if ((bool)e.NewValue)
		{
			_debounceTimer.Tick -= NormalizeNumericInput;
			_debounceTimer.Tick += NormalizeNumericInput;
		}
		else
		{
			_debounceTimer.Tick -= NormalizeNumericInput;
		}
	}

	public void FocusVkbTextBox()
	{
		if (VkbTextBox.IsLoaded && VkbTextBox.IsVisible)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
			{
				VkbTextBox.Focus();
				Keyboard.Focus(VkbTextBox);
				VkbTextBox.CaretIndex = Math.Max(0, VkbTextBox.Text.Length);
			});
		}
	}

	public void FocusVkbPasswordBox()
	{
		if (VkbPasswordBox.IsLoaded && VkbPasswordBox.IsVisible)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
			{
				VkbPasswordBox.Focus();
				Keyboard.Focus(VkbPasswordBox);
			});
		}
	}

	private void OnKeyboardLanguageChangedHandler(object? sender, eLanguageCode e)
	{
		SetInputMethod();
	}

	private void SetInputMethod()
	{
		switch (CurrentLang)
		{
			case eLanguageCode.en_US:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
				break;
			case eLanguageCode.vi_VN:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
				InputMethod.SetPreferredImeState(VkbTextBox, InputMethodState.On);
				InputMethod.SetPreferredImeConversionMode(VkbTextBox, ImeConversionModeValues.Alphanumeric);
				break;
			case eLanguageCode.ko_KR:
			case eLanguageCode.zh_CN:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, true);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, false);
				InputMethod.SetPreferredImeState(VkbTextBox, InputMethodState.On);
				InputMethod.SetPreferredImeConversionMode(VkbTextBox, ImeConversionModeValues.Native);
				break;
		}
	}

	public void UpdateNumericLabelVisibility()
	{
		var showMinMax = Layout != eVkLayout.Text && (Minimum != decimal.MinValue || Maximum != decimal.MaxValue);
		if (Layout == eVkLayout.Numeric)
		{
			MinTbl.Visibility = showMinMax && Minimum != int.MinValue ? Visibility.Visible : Visibility.Collapsed;
			MaxTbl.Visibility = showMinMax && Maximum != int.MaxValue ? Visibility.Visible : Visibility.Collapsed;
		}
		else if (Layout == eVkLayout.Decimal)
		{
			MinTbl.Visibility = showMinMax && Minimum != decimal.MinValue ? Visibility.Visible : Visibility.Collapsed;
			MaxTbl.Visibility = showMinMax && Maximum != decimal.MaxValue ? Visibility.Visible : Visibility.Collapsed;
		}
		else
		{
			MinTbl.Visibility = Visibility.Collapsed;
			MaxTbl.Visibility = Visibility.Collapsed;
		}
	}

	public void RemoveNumericHandler()
	{
		VkbTextBox.PreviewTextInput -= VkbTextBox_PreviewTextInput;
		VkbTextBox.TextChanged -= VkbTextBox_TextChanged;
		DataObject.RemovePastingHandler(VkbTextBox, VkbTextBox_OnTextBoxPasting);
	}

	public void AddOrRemoveNumericHandler()
	{
		RemoveNumericHandler();

		if (Layout != eVkLayout.Text)
		{
			VkbTextBox.PreviewTextInput += VkbTextBox_PreviewTextInput;
			VkbTextBox.TextChanged += VkbTextBox_TextChanged;
			DataObject.AddPastingHandler(VkbTextBox, VkbTextBox_OnTextBoxPasting);
		}
	}

	public void UpdateSourceBinding()
	{
		VkbTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
	}

	private void VkbTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		_debounceTimer.Stop();

		if (!InternalUpdate)
		{
			_debounceTimer.Start();
		}

		if (InternalUpdate)
		{
			InternalUpdate = false;
		}
	}

	private void NormalizeNumericInput(object? sender, EventArgs eventArgs)
	{
		var txt = VkbTextBox.Text;
		if (Layout == eVkLayout.Numeric)
		{
			if (decimal.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval))
			{
				var clamped = Math.Clamp(intval, Minimum, Maximum);
				SetText(() =>
				{
					VkbTextBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
					VkbTextBox.CaretIndex = VkbTextBox.Text.Length;
				});
			}
		}
		else if (Layout == eVkLayout.Decimal)
		{
			if (decimal.TryParse(txt, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval))
			{
				decimal clamped = Math.Clamp(decval, Minimum, Maximum);
				if (!txt.EndsWith(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
				{
					SetText(() =>
					{
						FormatDecimalValue(clamped);
					});
				}
			}
		}

		_debounceTimer.Stop();
	}

	private void FormatDecimalValue(decimal decimalVal)
	{
		SetText(() =>
		{
			VkbTextBox.Text = string.IsNullOrEmpty(DecimalFormat) ? decimalVal.ToString(CultureInfo.InvariantCulture) : decimalVal.ToString(DecimalFormat, CultureInfo.InvariantCulture);
			VkbTextBox.CaretIndex = VkbTextBox.Text.Length;
		});
	}

	private void VkbTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		if (Layout == eVkLayout.Decimal)
		{
			var sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;

			if (e.Text == sep && !VkbTextBox.Text.Contains(sep))
			{
				e.Handled = false;
				return;
			}
		}
		var proposedText = VkbTextBox.Text.Insert(VkbTextBox.CaretIndex, e.Text);
		e.Handled = !IsValidNumericInput(proposedText);
	}

	private void VkbTextBox_OnTextBoxPasting(object sender, DataObjectPastingEventArgs e)
	{
		if (e.DataObject.GetDataPresent(DataFormats.Text))
		{
			var text = (string)e.DataObject.GetData(DataFormats.Text);
			if (!IsValidNumericInput(text))
				e.CancelCommand();
		}
		else
			e.CancelCommand();
	}

	private void SetText(Action action)
	{
		InternalUpdate = true;
		action.Invoke();
	}

	private bool IsValidNumericInput(string input)
	{
		if (Layout == eVkLayout.Numeric)
			return Regex.IsMatch(input, @"^-?[0-9]*$");
		else
		{
			var sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
			string esc = Regex.Escape(sep);
			return Regex.IsMatch(input, @"^-?[0-9]*(" + esc + @"[0-9]*)?$");
		}
	}

	public void ClampNumericValue(bool updateSourceBinding = false)
	{
		if (Layout == eVkLayout.Numeric)
		{
			decimal.TryParse(VkbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
			var clamped = Math.Clamp(intval, Minimum, Maximum);
			SetText(() =>
			{
				VkbTextBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
				VkbTextBox.CaretIndex = VkbTextBox.Text.Length;
			});
		}
		else if (Layout == eVkLayout.Decimal)
		{
			decimal.TryParse(VkbTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval);
			decimal clamped = Math.Clamp(decval, Minimum, Maximum);
			if (!VkbTextBox.Text.EndsWith(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
			{
				SetText(() =>
				{
					FormatDecimalValue(clamped);
				});
			}
		}

		if (updateSourceBinding)
		{
			VkbTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
		}
	}

	public bool IsOuterRange()
	{
		if (Layout == eVkLayout.Numeric)
		{
			var isSuccess = decimal.TryParse(VkbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
			if (!isSuccess || intval < Minimum || intval > Maximum)
			{
				return true;
			}
		}
		else if (Layout == eVkLayout.Decimal)
		{
			var isSuccess = decimal.TryParse(VkbTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval);
			if (!isSuccess || decval < Minimum || decval > Maximum)
			{
				return true;
			}
		}

		return false;
	}
}

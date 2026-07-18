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

/// <summary>
/// \if KO
/// <para>텍스트·정수·소수 입력 UI와 범위 제한, 바인딩, 포커스 및 입력기 제어를 결합한 가상 키보드입니다.</para>
/// \endif
/// \if EN
/// <para>Represents a virtual keyboard UI combining text, integer, and decimal input with range, binding, focus, and IME control.</para>
/// \endif
/// </summary>
public partial class DreamineVirtualKeyboardUI : DreamineVirtualKeyboard
{
	/// <summary>
	/// \if KO
	/// <para>숫자 입력 정규화를 지연하는 디바운스 타이머입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stores the debounce timer that delays numeric normalization.</para>
	/// \endif
	/// </summary>
	private readonly DispatcherTimer _debounceTimer;

	/// <summary>
	/// \if KO
	/// <para>텍스트 변경이 내부 갱신에서 시작되었는지 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets whether a text change originated from an internal update.</para>
	/// \endif
	/// </summary>
	public bool InternalUpdate { get; set; } = false;

	/// <summary>
	/// \if KO
	/// <para>키보드가 편집하는 문자열 값을 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets the string value edited by the keyboard.</para>
	/// \endif
	/// </summary>
	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// \if KO
	/// <para>허용할 최소 숫자 값을 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets the minimum allowed numeric value.</para>
	/// \endif
	/// </summary>
	public decimal Minimum
	{
		get => (decimal)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	/// <summary>
	/// \if KO
	/// <para>허용할 최대 숫자 값을 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets the maximum allowed numeric value.</para>
	/// \endif
	/// </summary>
	public decimal Maximum
	{
		get => (decimal)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	/// <summary>
	/// \if KO
	/// <para>소수 값을 표시할 선택적 .NET 숫자 형식 문자열을 가져오거나 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Gets or sets an optional .NET numeric format string for decimal display.</para>
	/// \endif
	/// </summary>
	public string DecimalFormat { get; set; } = string.Empty;

	/// <summary>
	/// \if KO
	/// <para>양방향 바인딩되는 <see cref="Value"/> 종속성 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the two-way-bindable <see cref="Value"/> dependency property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value), typeof(string), typeof(DreamineVirtualKeyboardUI),
		new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	/// <summary>
	/// \if KO
	/// <para><see cref="Minimum"/> 종속성 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the <see cref="Minimum"/> dependency property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum), typeof(decimal), typeof(DreamineVirtualKeyboardUI),
		new PropertyMetadata(decimal.MinValue));

	/// <summary>
	/// \if KO
	/// <para><see cref="Maximum"/> 종속성 속성을 식별합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Identifies the <see cref="Maximum"/> dependency property.</para>
	/// \endif
	/// </summary>
	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum), typeof(decimal), typeof(DreamineVirtualKeyboardUI),
		new PropertyMetadata(decimal.MaxValue));

	/// <summary>
	/// \if KO
	/// <para>XAML 구성 요소, 디바운스 타이머, 입력 이벤트 및 언어별 입력기 상태를 초기화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Initializes XAML components, the debounce timer, input events, and language-specific IME state.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>텍스트 및 암호 입력 상자의 로드·언로드·표시 상태 이벤트를 등록합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Registers loaded, unloaded, and visibility events for text and password inputs.</para>
	/// \endif
	/// </summary>
	private void RegisterVkbInputEvents()
	{
		VkbTextBox.Loaded += VkbInput_Loaded;
		VkbTextBox.Unloaded += VkbInput_Unloaded;
		VkbTextBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
		VkbPasswordBox.Loaded += VkbInput_Loaded;
		VkbPasswordBox.Unloaded += VkbInput_Unloaded;
		VkbPasswordBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
	}

	/// <summary>
	/// \if KO
	/// <para>입력 상자가 로드되면 포커스를 설정하고 숫자 입력을 필요 시 범위 안으로 보정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Focuses loaded inputs and clamps numeric input when necessary.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>로드된 입력 요소입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The loaded input element.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>로드 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Loaded-event data.</para>
	/// \endif
	/// </param>
	private void VkbInput_Loaded(object sender, RoutedEventArgs e)
	{
		FocusVkbTextBox();
		FocusVkbPasswordBox();
		InternalUpdate = true;
		if (Layout != VkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || IsOuterRange()))
		{
			ClampNumericValue(true);
		}
	}

	/// <summary>
	/// \if KO
	/// <para>입력 상자가 언로드되면 숫자 정규화 타이머와 이벤트를 중지합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Stops the numeric-normalization timer and event when an input unloads.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>언로드된 입력 요소입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The unloaded input element.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>언로드 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Unloaded-event data.</para>
	/// \endif
	/// </param>
	private void VkbInput_Unloaded(object sender, RoutedEventArgs e)
	{
		InternalUpdate = true;
		_debounceTimer.Stop();
		_debounceTimer.Tick -= NormalizeNumericInput;
	}

	/// <summary>
	/// \if KO
	/// <para>입력 표시 상태에 따라 포커스, 숫자 보정 및 디바운스 Tick 구독을 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Updates focus, numeric clamping, and debounce Tick subscription for input visibility changes.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>표시 상태가 변경된 입력 요소입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The input element whose visibility changed.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>새 표시 상태를 포함하는 변경 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Change data containing the new visibility state.</para>
	/// \endif
	/// </param>
	/// <exception cref="InvalidCastException">
	/// \if KO
	/// <para>새 값이 <see cref="bool"/>이 아닐 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when the new value is not a <see cref="bool"/>.</para>
	/// \endif
	/// </exception>
	private void VkbInput_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		FocusVkbTextBox();
		FocusVkbPasswordBox();

		if (Layout != VkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || IsOuterRange()))
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

	/// <summary>
	/// \if KO
	/// <para>텍스트 입력 상자가 표시되면 유휴 시점에 포커스하고 캐럿을 끝으로 이동합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Focuses the visible text input at idle priority and moves the caret to the end.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>암호 입력 상자가 표시되면 유휴 시점에 키보드 포커스를 설정합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Assigns keyboard focus to the visible password input at idle priority.</para>
	/// \endif
	/// </summary>
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

	/// <summary>
	/// \if KO
	/// <para>키보드 언어 변경 시 WPF 입력기 설정을 다시 적용합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Reapplies WPF input-method settings when the keyboard language changes.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>언어 변경 이벤트 발신자입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The language-change event source.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>새 언어 코드입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The new language code.</para>
	/// \endif
	/// </param>
	private void OnKeyboardLanguageChangedHandler(object? sender, LanguageCode e)
	{
		SetInputMethod();
	}

	/// <summary>
	/// \if KO
	/// <para>현재 언어에 따라 텍스트 입력 상자의 WPF 입력기를 활성화하거나 중지합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Enables or suspends the WPF input method for the text box according to the current language.</para>
	/// \endif
	/// </summary>
	private void SetInputMethod()
	{
		switch (CurrentLang)
		{
			case LanguageCode.en_US:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
				break;
			case LanguageCode.vi_VN:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
				break;
			case LanguageCode.ko_KR:
			case LanguageCode.zh_CN:
				InputMethod.SetIsInputMethodEnabled(VkbTextBox, true);
				InputMethod.SetIsInputMethodSuspended(VkbTextBox, false);
				break;
		}
	}

	/// <summary>
	/// \if KO
	/// <para>현재 레이아웃과 제한값에 따라 최소·최대 안내 레이블 표시 상태를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Updates minimum and maximum label visibility according to layout and configured limits.</para>
	/// \endif
	/// </summary>
	public void UpdateNumericLabelVisibility()
	{
		var showMinMax = Layout != VkLayout.Text && (Minimum != decimal.MinValue || Maximum != decimal.MaxValue);
		if (Layout == VkLayout.Numeric)
		{
			MinTbl.Visibility = showMinMax && Minimum != int.MinValue ? Visibility.Visible : Visibility.Collapsed;
			MaxTbl.Visibility = showMinMax && Maximum != int.MaxValue ? Visibility.Visible : Visibility.Collapsed;
		}
		else if (Layout == VkLayout.Decimal)
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

	/// <summary>
	/// \if KO
	/// <para>텍스트 입력 상자에서 숫자 입력 검증 처리기를 제거합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Removes numeric-input validation handlers from the text box.</para>
	/// \endif
	/// </summary>
	public void RemoveNumericHandler()
	{
		VkbTextBox.PreviewTextInput -= VkbTextBox_PreviewTextInput;
		VkbTextBox.TextChanged -= VkbTextBox_TextChanged;
		DataObject.RemovePastingHandler(VkbTextBox, VkbTextBox_OnTextBoxPasting);
	}

	/// <summary>
	/// \if KO
	/// <para>기존 숫자 처리기를 제거하고 비텍스트 레이아웃이면 다시 등록합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Removes existing numeric handlers and reattaches them for non-text layouts.</para>
	/// \endif
	/// </summary>
	public void AddOrRemoveNumericHandler()
	{
		RemoveNumericHandler();

		if (Layout != VkLayout.Text)
		{
			VkbTextBox.PreviewTextInput += VkbTextBox_PreviewTextInput;
			VkbTextBox.TextChanged += VkbTextBox_TextChanged;
			DataObject.AddPastingHandler(VkbTextBox, VkbTextBox_OnTextBoxPasting);
		}
	}

	/// <summary>
	/// \if KO
	/// <para>텍스트 바인딩의 현재 값을 소스에 즉시 반영합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Immediately updates the source of the text binding with the current value.</para>
	/// \endif
	/// </summary>
	public void UpdateSourceBinding()
	{
		VkbTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
	}

	/// <summary>
	/// \if KO
	/// <para>외부가 아닌 텍스트 변경이면 숫자 정규화 타이머를 다시 시작합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Restarts numeric normalization debounce for text changes not initiated internally.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>변경된 텍스트 상자입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The changed text box.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>텍스트 변경 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Text-change data.</para>
	/// \endif
	/// </param>
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

	/// <summary>
	/// \if KO
	/// <para>지연된 숫자 입력을 해석하고 범위 제한 및 소수 형식을 적용합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Parses delayed numeric input and applies range clamping and decimal formatting.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>타이머 이벤트 발신자입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The timer-event source.</para>
	/// \endif
	/// </param>
	/// <param name="eventArgs">
	/// \if KO
	/// <para>타이머 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Timer-event data.</para>
	/// \endif
	/// </param>
	/// <exception cref="ArgumentException">
	/// \if KO
	/// <para><see cref="Minimum"/>이 <see cref="Maximum"/>보다 클 때 <see cref="Math.Clamp(decimal, decimal, decimal)"/>에서 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown by <see cref="Math.Clamp(decimal, decimal, decimal)"/> when <see cref="Minimum"/> exceeds <see cref="Maximum"/>.</para>
	/// \endif
	/// </exception>
	private void NormalizeNumericInput(object? sender, EventArgs eventArgs)
	{
		var txt = VkbTextBox.Text;
		if (Layout == VkLayout.Numeric)
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
		else if (Layout == VkLayout.Decimal)
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

	/// <summary>
	/// \if KO
	/// <para>지정한 소수 값을 구성된 형식 또는 고정 문화권 기본 형식으로 표시합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Displays a decimal value using the configured format or invariant default formatting.</para>
	/// \endif
	/// </summary>
	/// <param name="decimalVal">
	/// \if KO
	/// <para>표시할 소수 값입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The decimal value to display.</para>
	/// \endif
	/// </param>
	/// <exception cref="FormatException">
	/// \if KO
	/// <para><see cref="DecimalFormat"/>이 유효하지 않을 때 발생할 수 있습니다.</para>
	/// \endif
	/// \if EN
	/// <para>May be thrown when <see cref="DecimalFormat"/> is invalid.</para>
	/// \endif
	/// </exception>
	private void FormatDecimalValue(decimal decimalVal)
	{
		SetText(() =>
		{
			VkbTextBox.Text = string.IsNullOrEmpty(DecimalFormat) ? decimalVal.ToString(CultureInfo.InvariantCulture) : decimalVal.ToString(DecimalFormat, CultureInfo.InvariantCulture);
			VkbTextBox.CaretIndex = VkbTextBox.Text.Length;
		});
	}

	/// <summary>
	/// \if KO
	/// <para>입력 예정 문자열이 현재 숫자 레이아웃 형식에 맞는지 검사하여 입력을 허용하거나 차단합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Allows or blocks text input by validating the proposed string against the current numeric layout.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>텍스트 입력 요소입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The text input element.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>입력 문자와 처리 상태를 포함하는 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Data containing input text and handled state.</para>
	/// \endif
	/// </param>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="e"/>가 <see langword="null"/>일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="e"/> is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	private void VkbTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		if (Layout == VkLayout.Decimal)
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

	/// <summary>
	/// \if KO
	/// <para>붙여넣을 텍스트가 현재 숫자 형식에 맞지 않으면 붙여넣기 명령을 취소합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Cancels a paste command when pasted text does not match the current numeric format.</para>
	/// \endif
	/// </summary>
	/// <param name="sender">
	/// \if KO
	/// <para>붙여넣기 대상입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The paste target.</para>
	/// \endif
	/// </param>
	/// <param name="e">
	/// \if KO
	/// <para>붙여넣기 데이터와 취소 동작을 제공하는 이벤트 데이터입니다.</para>
	/// \endif
	/// \if EN
	/// <para>Event data providing paste data and cancellation.</para>
	/// \endif
	/// </param>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="e"/> 또는 해당 데이터 객체가 <see langword="null"/>일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="e"/> or its data object is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	/// <exception cref="InvalidCastException">
	/// \if KO
	/// <para>텍스트 데이터가 문자열로 변환되지 않을 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when text data cannot be cast to a string.</para>
	/// \endif
	/// </exception>
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

	/// <summary>
	/// \if KO
	/// <para>내부 갱신 상태를 설정하고 지정한 텍스트 변경 동작을 실행합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Marks an internal update and invokes the specified text-changing action.</para>
	/// \endif
	/// </summary>
	/// <param name="action">
	/// \if KO
	/// <para>실행할 텍스트 변경 동작입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The text-changing action to invoke.</para>
	/// \endif
	/// </param>
	/// <exception cref="NullReferenceException">
	/// \if KO
	/// <para><paramref name="action"/>이 <see langword="null"/>일 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <paramref name="action"/> is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	private void SetText(Action action)
	{
		InternalUpdate = true;
		action.Invoke();
	}

	/// <summary>
	/// \if KO
	/// <para>문자열이 현재 정수 또는 소수 레이아웃에서 부분 입력으로 유효한지 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether a string is valid partial input for the current integer or decimal layout.</para>
	/// \endif
	/// </summary>
	/// <param name="input">
	/// \if KO
	/// <para>검사할 입력 문자열입니다.</para>
	/// \endif
	/// \if EN
	/// <para>The input string to validate.</para>
	/// \endif
	/// </param>
	/// <returns>
	/// \if KO
	/// <para>현재 숫자 패턴과 일치하면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> if the input matches the current numeric pattern.</para>
	/// \endif
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// \if KO
	/// <para><paramref name="input"/>이 <see langword="null"/>일 때 정규식 검사에서 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown by regular-expression matching when <paramref name="input"/> is <see langword="null"/>.</para>
	/// \endif
	/// </exception>
	private bool IsValidNumericInput(string input)
	{
		if (Layout == VkLayout.Numeric)
			return Regex.IsMatch(input, @"^-?[0-9]*$");
		else
		{
			var sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
			string esc = Regex.Escape(sep);
			return Regex.IsMatch(input, @"^-?[0-9]*(" + esc + @"[0-9]*)?$");
		}
	}

	/// <summary>
	/// \if KO
	/// <para>현재 숫자 텍스트를 해석하여 최소·최대 범위로 제한하고 선택적으로 바인딩 소스를 갱신합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Parses current numeric text, clamps it to minimum and maximum bounds, and optionally updates the binding source.</para>
	/// \endif
	/// </summary>
	/// <param name="updateSourceBinding">
	/// \if KO
	/// <para>보정 후 바인딩 소스를 즉시 갱신하려면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> to update the binding source immediately after clamping.</para>
	/// \endif
	/// </param>
	/// <exception cref="ArgumentException">
	/// \if KO
	/// <para><see cref="Minimum"/>이 <see cref="Maximum"/>보다 클 때 발생합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Thrown when <see cref="Minimum"/> exceeds <see cref="Maximum"/>.</para>
	/// \endif
	/// </exception>
	public void ClampNumericValue(bool updateSourceBinding = false)
	{
		if (Layout == VkLayout.Numeric)
		{
			decimal.TryParse(VkbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
			var clamped = Math.Clamp(intval, Minimum, Maximum);
			SetText(() =>
			{
				VkbTextBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
				VkbTextBox.CaretIndex = VkbTextBox.Text.Length;
			});
		}
		else if (Layout == VkLayout.Decimal)
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

	/// <summary>
	/// \if KO
	/// <para>현재 숫자 텍스트가 해석 불가능하거나 구성된 범위를 벗어났는지 확인합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Determines whether current numeric text is unparseable or outside configured bounds.</para>
	/// \endif
	/// </summary>
	/// <returns>
	/// \if KO
	/// <para>숫자 또는 소수 레이아웃에서 입력이 잘못되었거나 범위를 벗어나면 <see langword="true"/>입니다.</para>
	/// \endif
	/// \if EN
	/// <para><see langword="true"/> when input is invalid or out of range for integer or decimal layouts.</para>
	/// \endif
	/// </returns>
	public bool IsOuterRange()
	{
		if (Layout == VkLayout.Numeric)
		{
			var isSuccess = decimal.TryParse(VkbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
			if (!isSuccess || intval < Minimum || intval > Maximum)
			{
				return true;
			}
		}
		else if (Layout == VkLayout.Decimal)
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

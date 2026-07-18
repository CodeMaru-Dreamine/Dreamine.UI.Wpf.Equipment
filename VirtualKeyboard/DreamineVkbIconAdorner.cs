using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>장식 대상 오른쪽 아래에 클릭 가능한 가상 키보드 아이콘을 그립니다.</para>
/// \endif
/// \if EN
/// <para>Draws a clickable virtual-keyboard icon at the lower-right of an adorned element.</para>
/// \endif
/// </summary>
public sealed class DreamineVkbIconAdorner : Adorner
{
    /// <summary>
    /// \if KO
    /// <para>미리보기 마우스 누름 시 실행할 동작을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the action invoked on preview mouse down.</para>
    /// \endif
    /// </summary>
    private Action<object?, MouseButtonEventArgs>? _previewMouseDownAction;

    /// <summary>
    /// \if KO
    /// <para>지정한 UI 요소를 장식하는 새 아이콘 장식자를 만듭니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes an icon adorner for the specified UI element.</para>
    /// \endif
    /// </summary>
    /// <param name="adornedElement">
    /// \if KO
    /// <para>아이콘을 표시할 대상 요소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The element on which the icon is displayed.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="adornedElement"/>가 <see langword="null"/>일 때 기본 생성자에서 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown by the base constructor when <paramref name="adornedElement"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public DreamineVkbIconAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = true;
        Cursor = Cursors.Hand;
        PreviewMouseDown += OnPreviewMouseDown;
    }

    /// <summary>
    /// \if KO
    /// <para>아이콘을 누를 때 실행할 동작을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the action invoked when the icon is pressed.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>실행할 마우스 입력 동작입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The mouse-input action to invoke.</para>
    /// \endif
    /// </param>
    public void SetPreviewMouseDownAction(Action<object?, MouseButtonEventArgs> action)
        => _previewMouseDownAction = action;

    /// <summary>
    /// \if KO
    /// <para>대상 크기에 맞춰 키보드 모양 아이콘을 렌더링합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Renders a keyboard-shaped icon relative to the adorned element's size.</para>
    /// \endif
    /// </summary>
    /// <param name="drawingContext">
    /// \if KO
    /// <para>아이콘을 그릴 렌더링 컨텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The drawing context used to render the icon.</para>
    /// \endif
    /// </param>
    /// <exception cref="NullReferenceException">
    /// \if KO
    /// <para><paramref name="drawingContext"/>가 <see langword="null"/>일 때 발생할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>May be thrown when <paramref name="drawingContext"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var adornedSize = AdornedElement.RenderSize;
        const double width = 34;
        const double height = 22;
        var rect = new Rect(
            Math.Max(0, adornedSize.Width - width - 4),
            Math.Max(0, adornedSize.Height - height - 4),
            width,
            height);

        var background = new SolidColorBrush(Color.FromArgb(230, 45, 52, 64));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(120, 140, 160)), 1);
        drawingContext.DrawRoundedRectangle(background, border, rect, 4, 4);

        var keyBrush = new SolidColorBrush(Color.FromRgb(230, 236, 244));
        const double keySize = 4;
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 5; col++)
            {
                var keyRect = new Rect(rect.Left + 5 + col * 5, rect.Top + 5 + row * 6, keySize, keySize);
                drawingContext.DrawRoundedRectangle(keyBrush, null, keyRect, 1, 1);
            }
        }

        drawingContext.DrawRoundedRectangle(
            keyBrush,
            null,
            new Rect(rect.Left + 8, rect.Top + 17, 18, 2),
            1,
            1);
    }

    /// <summary>
    /// \if KO
    /// <para>등록된 클릭 동작을 실행하고 입력 이벤트를 처리됨으로 표시합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Invokes the registered click action and marks the input event as handled.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>마우스 이벤트 발신자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The mouse-event source.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>마우스 버튼 이벤트 데이터입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Mouse-button event data.</para>
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
    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _previewMouseDownAction?.Invoke(sender, e);
        e.Handled = true;
    }
}

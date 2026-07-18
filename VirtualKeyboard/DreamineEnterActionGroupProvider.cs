using System.Windows;
using System.Windows.Controls;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>그룹 식별자와 Enter 확정 동작 공급자를 XAML 콘텐츠로 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a group identifier and Enter-commit action provider as XAML content.</para>
/// \endif
/// </summary>
public class DreamineEnterActionGroupProvider : ContentControl
{
    /// <summary>
    /// \if KO
    /// <para>Enter 동작 그룹 식별자를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the Enter-action group identifier.</para>
    /// \endif
    /// </summary>
    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }

    /// <summary>
    /// \if KO
    /// <para><see cref="GroupId"/> 종속성 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the <see cref="GroupId"/> dependency property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty GroupIdProperty =
        DependencyProperty.Register(nameof(GroupId), typeof(string), typeof(DreamineEnterActionGroupProvider), new PropertyMetadata(null));

    /// <summary>
    /// \if KO
    /// <para>Enter 키로 실행할 확정 동작 공급자를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the commit-action provider invoked by Enter.</para>
    /// \endif
    /// </summary>
    public IEnterActionProvider? Commit
    {
        get => (IEnterActionProvider?)GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    /// <summary>
    /// \if KO
    /// <para><see cref="Commit"/> 종속성 속성을 식별합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Identifies the <see cref="Commit"/> dependency property.</para>
    /// \endif
    /// </summary>
    public static readonly DependencyProperty CommitProperty = DependencyProperty.Register(nameof(Commit), typeof(IEnterActionProvider), typeof(DreamineEnterActionGroupProvider), new PropertyMetadata(null));
}

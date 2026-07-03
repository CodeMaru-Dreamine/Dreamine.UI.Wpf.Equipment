using System.Windows;
using System.Windows.Controls;
using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public class DreamineEnterActionGroupProvider : ContentControl
{
    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }

    public static readonly DependencyProperty GroupIdProperty =
        DependencyProperty.Register(nameof(GroupId), typeof(string), typeof(DreamineEnterActionGroupProvider), new PropertyMetadata(null));

    public IEnterActionProvider? Commit
    {
        get => (IEnterActionProvider?)GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    public static readonly DependencyProperty CommitProperty = DependencyProperty.Register(nameof(Commit), typeof(IEnterActionProvider), typeof(DreamineEnterActionGroupProvider), new PropertyMetadata(null));
}

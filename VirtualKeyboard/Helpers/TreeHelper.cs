using System.Windows;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

public static class TreeHelper
{
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject dep) where T : DependencyObject
    {
        if (dep == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
        {
            var child = VisualTreeHelper.GetChild(dep, i);
            if (child is T t)
                yield return t;

            foreach (var childOfChild in FindVisualChildren<T>(child))
                yield return childOfChild;
        }
    }

    public static T? FindFirstVisualChild<T>(this DependencyObject dep, string name) where T : FrameworkElement
    {
        if (dep == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
        {
            var child = VisualTreeHelper.GetChild(dep, i);

            if (child is T t && t.Name == name)
                return t;

            var result = FindFirstVisualChild<T>(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    public static T? FindFirstVisualChild<T>(this DependencyObject dep) where T : FrameworkElement
    {
        if (dep == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
        {
            var child = VisualTreeHelper.GetChild(dep, i);

            if (child is T t)
                return t;

            var result = FindFirstVisualChild<T>(child);
            if (result != null)
                return result;
        }

        return null;
    }

    public static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);

        while (parentObject != null)
        {
            if (parentObject is T parent)
                return parent;

            parentObject = VisualTreeHelper.GetParent(parentObject);
        }

        return null!;
    }
}

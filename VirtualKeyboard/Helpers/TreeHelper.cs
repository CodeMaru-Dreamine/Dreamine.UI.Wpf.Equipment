using System.Windows;
using System.Windows.Media;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

/// <summary>
/// \if KO
/// <para>WPF 시각적 트리에서 자식과 부모 요소를 찾는 확장 메서드를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides extension methods for locating child and parent elements in a WPF visual tree.</para>
/// \endif
/// </summary>
public static class TreeHelper
{
    /// <summary>
    /// \if KO
    /// <para>지정한 형식의 모든 시각적 후손을 깊이 우선으로 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns all visual descendants of the specified type using depth-first traversal.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>찾을 종속성 객체 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The dependency-object type to find.</para>
    /// \endif
    /// </typeparam>
    /// <param name="dep">
    /// \if KO
    /// <para>탐색 시작 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The traversal root.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>일치하는 후손의 지연 시퀀스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A deferred sequence of matching descendants.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>지정한 이름과 형식에 일치하는 첫 시각적 후손을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the first visual descendant matching the specified name and type.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>찾을 프레임워크 요소 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The framework-element type to find.</para>
    /// \endif
    /// </typeparam>
    /// <param name="dep">
    /// \if KO
    /// <para>탐색 시작 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The traversal root.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>일치시킬 요소 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The element name to match.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>첫 일치 요소이며 없으면 <see langword="null"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The first match, or <see langword="null"/> if absent.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>지정한 형식에 일치하는 첫 시각적 후손을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the first visual descendant of the specified type.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>찾을 프레임워크 요소 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The framework-element type to find.</para>
    /// \endif
    /// </typeparam>
    /// <param name="dep">
    /// \if KO
    /// <para>탐색 시작 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The traversal root.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>첫 일치 요소이며 없으면 <see langword="null"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The first match, or <see langword="null"/> if absent.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>시각적 트리를 위로 탐색하여 지정한 형식의 첫 부모를 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Traverses upward through the visual tree to find the first parent of the specified type.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>찾을 부모 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The parent type to find.</para>
    /// \endif
    /// </typeparam>
    /// <param name="child">
    /// \if KO
    /// <para>탐색을 시작할 자식 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The child object at which to begin.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>첫 일치 부모이며 없으면 런타임 null입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The first matching parent, or runtime null if none exists.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para><paramref name="child"/>가 유효한 <see cref="Visual"/> 또는 <see cref="System.Windows.Media.Media3D.Visual3D"/>가 아닐 때 발생할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>May be thrown when <paramref name="child"/> is not a valid <see cref="Visual"/> or <see cref="System.Windows.Media.Media3D.Visual3D"/>.</para>
    /// \endif
    /// </exception>
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

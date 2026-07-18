using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard
{
    /// <summary>
    /// \if KO
    /// <para>전체 가상 키보드 레이아웃의 WPF 사용자 컨트롤입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the WPF user control for the full virtual-keyboard layout.</para>
    /// \endif
    /// </summary>
    public partial class DreamineFullKeyboardLayout : UserControl
    {
        /// <summary>
        /// \if KO
        /// <para>XAML 구성 요소를 초기화하여 전체 키보드 레이아웃을 만듭니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes XAML components for the full keyboard layout.</para>
        /// \endif
        /// </summary>
        public DreamineFullKeyboardLayout()
        {
            InitializeComponent();
        }
    }
}

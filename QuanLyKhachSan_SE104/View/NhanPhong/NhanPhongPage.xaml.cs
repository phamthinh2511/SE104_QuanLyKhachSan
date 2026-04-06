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
using QuanLyKhachSan_SE104.ViewModel;

namespace QuanLyKhachSan_SE104.View.NhanPhong
{
    /// <summary>
    /// Interaction logic for NhanPhongPage.xaml
    /// </summary>
    public partial class NhanPhongPage : UserControl
    {
        public NhanPhongPage()
        {
            InitializeComponent();
            this.DataContext = new NhanPhongViewModel();
        }
        private void ShowPopup_Click(object sender, RoutedEventArgs e)
        {
            PopupOverlay.Visibility = Visibility.Visible;
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            PopupOverlay.Visibility = Visibility.Collapsed;
        }
    }
}

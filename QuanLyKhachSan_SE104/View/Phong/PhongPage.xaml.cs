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

namespace QuanLyKhachSan_SE104.View.PhongView
{
    /// <summary>
    /// Interaction logic for PhongPage.xaml
    /// </summary>
    public partial class PhongPage : UserControl
    {
        public PhongPage()
        {
            InitializeComponent();
            this.DataContext = new QuanLyKhachSan_SE104.ViewModel.PhongVM.PhongViewModel();
        }
    }
}

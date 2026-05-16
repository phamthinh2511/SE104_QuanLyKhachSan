using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhong;
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
using System.Windows.Shapes;
using QuanLyKhachSan_SE104.ViewModel;

namespace QuanLyKhachSan_SE104.View
{
    /// <summary>
    /// Interaction logic for HoaDonChiTietWindow.xaml
    /// </summary>
    public partial class HoaDonChiTietWindow : Window
    {
        public HoaDonChiTietWindow(HoaDonChiTietViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            if (viewModel.CloseAction == null)
            {
                viewModel.CloseAction = new Action(this.Close);
            }
        }
    }
}

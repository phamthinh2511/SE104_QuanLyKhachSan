using QuanLyKhachSan_SE104.ViewModel.KhachHangVM;
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

namespace QuanLyKhachSan_SE104.View.KhachHang
{
    /// <summary>
    /// Interaction logic for KhachHangCRUD.xaml
    /// </summary>
    public partial class KhachHangCRUD : Window
    {
        public KhachHangCRUD(KhachHangCRUDViewModel viewModel)
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

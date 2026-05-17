using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM;
using System.Windows;

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

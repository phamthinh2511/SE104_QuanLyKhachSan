using QuanLyKhachSan_SE104.ViewModel.DichVuVM;
using System.Windows;

namespace QuanLyKhachSan_SE104.View.DichVu
{
    public partial class DichVuPage : Window
    {
        public DichVuPage()
        {
            InitializeComponent();
            var vm = new DichVuViewModel();
            vm.CloseAction = () => this.Close();
            DataContext = vm;
        }
    }
}
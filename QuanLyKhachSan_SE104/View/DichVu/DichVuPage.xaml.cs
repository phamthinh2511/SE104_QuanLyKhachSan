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
            // Gán CloseAction để ViewModel có thể đóng Window
            vm.CloseAction = () => this.Close();
            this.DataContext = vm;
        }
    }
}
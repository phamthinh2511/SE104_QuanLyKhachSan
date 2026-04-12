using QuanLyKhachSan_SE104.ViewModel.Dashboard;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.View.Dashboard
{
    public partial class DashboardPage : UserControl
    {
        public DashboardPage()
        {
            InitializeComponent();
            this.DataContext = new DashboardViewModel();
        }
    }
}

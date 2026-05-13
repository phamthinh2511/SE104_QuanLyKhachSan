using System.Windows;
using QuanLyKhachSan_SE104.ViewModel;

namespace QuanLyKhachSan_SE104.View
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel();
        }
    }
}
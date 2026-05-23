using QuanLyKhachSan_SE104.View.Login;
using QuanLyKhachSan_SE104.ViewModel.MainViewModel;
using System.Windows;

namespace QuanLyKhachSan_SE104
{
    public partial class MainWindow : Window
    {
        public MainWindow() : this(null)
        {
        }

        public MainWindow(QuanLyKhachSan_SE104.Model.TaiKhoan account)
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(account);
        }

        private void BtnUserMenu_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = !UserMenuPopup.IsOpen;
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = false;
            var vm = DataContext as MainViewModel;
            var win = new DoiMatKhau(vm?.CurrentUsername ?? "");
            win.Owner = this;
            win.ShowDialog();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserMenuPopup.IsOpen = false;
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
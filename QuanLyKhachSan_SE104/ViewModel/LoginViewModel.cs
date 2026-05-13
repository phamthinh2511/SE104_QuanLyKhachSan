using System.ComponentModel; // Thư viện mặc định của hệ thống
using System.Linq;
using System.Runtime.CompilerServices; // Thư viện mặc định của hệ thống
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View;

namespace QuanLyKhachSan_SE104.ViewModel
{
    // Đã thay BaseViewModel thành INotifyPropertyChanged mặc định
    public class LoginViewModel : INotifyPropertyChanged
    {
        // Khởi tạo sự kiện OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; set; }
        public ICommand OpenForgotPasswordCommand { get; set; }

        public LoginViewModel()
        {
            // Đã đảo vị trí: Hành động đưa lên trước, Điều kiện (true) để ra sau
            LoginCommand = new RelayCommand<PasswordBox>((p) => Login(p), (p) => true);
            OpenForgotPasswordCommand = new RelayCommand<object>((p) => OpenForgotPassword(), (p) => true);
        }

        private void Login(PasswordBox p)
        {
            if (p == null) return;
            string password = p.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new QuanLyKhachSanContext())
            {
                var user = context.TaiKhoans.FirstOrDefault(x => x.Username == Username && x.PasswordHash == password);
                if (user != null)
                {
                    MainWindow main = new MainWindow();
                    main.Show();

                    Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
                }
                else
                {
                    MessageBox.Show("Sai tài khoản hoặc mật khẩu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenForgotPassword()
        {
            ForgotPasswordWindow forgotWin = new ForgotPasswordWindow();
            forgotWin.ShowDialog();
        }
    }
}
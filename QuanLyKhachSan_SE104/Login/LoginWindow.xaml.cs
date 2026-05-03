using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.View.Login
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Cho phép kéo cửa sổ ở vùng trống
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            };
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text?.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.",
                                "Thiếu thông tin",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = new QuanLyKhachSanContext();
                var account = context.TaiKhoans
                    .FirstOrDefault(t => t.Username == username && t.PasswordHash == password);

                if (account == null)
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.",
                                    "Sai thông tin đăng nhập",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message,
                                "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Vui lòng liên hệ quản trị viên để đặt lại mật khẩu.",
                            "Quên mật khẩu",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}

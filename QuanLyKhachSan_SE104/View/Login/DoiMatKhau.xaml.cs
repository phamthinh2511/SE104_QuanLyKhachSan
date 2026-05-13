using QuanLyKhachSan_SE104.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.View.Login
{
    public partial class DoiMatKhau : Window
    {
        private readonly string _username;
        private readonly QuanLyKhachSanContext _context;

        public DoiMatKhau(string username)
        {
            InitializeComponent();
            _username = username;
            _context = new QuanLyKhachSanContext();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        private void AnyPasswordChanged(object sender, RoutedEventArgs e)
        {
            lblOldPlaceholder.Visibility = txtOldPassword.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            lblNewPlaceholder.Visibility = txtNewPassword.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            lblConfirmPlaceholder.Visibility = txtConfirmPassword.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            lblError.Visibility = Visibility.Collapsed;
            lblSuccess.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            lblSuccess.Visibility = Visibility.Collapsed;

            var oldPwd = txtOldPassword.Password;
            var newPwd = txtNewPassword.Password;
            var confirmPwd = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(oldPwd) || string.IsNullOrEmpty(newPwd) || string.IsNullOrEmpty(confirmPwd))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin."); return;
            }
            if (newPwd.Length < 6)
            {
                ShowError("Mật khẩu mới phải có ít nhất 6 ký tự."); return;
            }
            if (newPwd != confirmPwd)
            {
                ShowError("Mật khẩu mới và xác nhận không khớp."); return;
            }
            if (oldPwd == newPwd)
            {
                ShowError("Mật khẩu mới không được trùng mật khẩu cũ."); return;
            }

            try
            {
                // Kiểm tra mật khẩu cũ với database
                var account = _context.TaiKhoans
                    .FirstOrDefault(t => t.Username == _username && t.PasswordHash == oldPwd);

                if (account == null)
                {
                    ShowError("Mật khẩu hiện tại không đúng."); return;
                }

                // Cập nhật mật khẩu mới
                account.PasswordHash = newPwd;
                _context.SaveChanges();

                lblSuccess.Text = "✅ Đổi mật khẩu thành công!";
                lblSuccess.Visibility = Visibility.Visible;

                btnConfirm.IsEnabled = false;

                // Tự đóng sau 1.5 giây
                var timer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, _) => { timer.Stop(); Close(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                ShowError("Lỗi kết nối cơ sở dữ liệu: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visibility = Visibility.Visible;
        }
    }
}
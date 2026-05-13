using System.Windows;

namespace QuanLyKhachSan_SE104.View
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsernameForgot.Text;
            if (string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Vui lòng nhập Username!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Do database chưa có Email để gửi mã xác nhận, tạm thời hiển thị thông báo
            MessageBox.Show($"Yêu cầu cấp lại mật khẩu cho tài khoản '{user}' đã được gửi tới Quản trị viên.",
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
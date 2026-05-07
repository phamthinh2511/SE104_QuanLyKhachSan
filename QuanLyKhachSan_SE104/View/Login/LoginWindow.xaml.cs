using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.View.Login
{
    public partial class LoginWindow : Window
    {
        private readonly QuanLyKhachSanContext _context;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new QuanLyKhachSanContext();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblUsernamePlaceholder.Visibility = string.IsNullOrEmpty(txtUsername.Text) ? Visibility.Visible : Visibility.Collapsed;
            lblError.Visibility = Visibility.Collapsed;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            lblPasswordPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
            lblError.Visibility = Visibility.Collapsed;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            // Simplified toggle password visibility: in WPF it's tricky to show password in a PasswordBox directly.
            // A common workaround is to use a TextBox overlapping the PasswordBox. For simplicity and standard security,
            // we will leave this as a UI placeholder or implement it if strongly needed.
            // Since WPF doesn't have a built-in PasswordBox.PasswordChar toggle to visible text easily, 
            // we will just focus on the core login logic for now.
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            // Disable button while processing
            btnLogin.IsEnabled = false;
            btnLogin.Content = "Đang đăng nhập...";

            try
            {
                // Authenticate with database
                var account = _context.TaiKhoans.FirstOrDefault(t => t.Username == username && t.PasswordHash == password);

                if (account != null)
                {
                    // Login successful
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    // Login failed
                    lblError.Text = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                    lblError.Visibility = Visibility.Visible;
                }
            }
            catch (System.Exception ex)
            {
                lblError.Text = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message;
                lblError.Visibility = Visibility.Visible;
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Đăng nhập";
            }
        }
    }
}

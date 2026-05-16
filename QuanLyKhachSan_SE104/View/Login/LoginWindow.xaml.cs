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
        private bool _isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new QuanLyKhachSanContext();
            
            // Tự động tạo tài khoản admin nếu chưa có
            try
            {
                if (!_context.TaiKhoans.Any(t => t.Username == "admin"))
                {
                    var nv = new NhanVien { HoTen = "Quản trị viên", ChucVu = true, TrangThaiLamViec = true };
                    _context.NhanViens.Add(nv);
                    _context.SaveChanges();

                    var tk = new TaiKhoan { Username = "admin", PasswordHash = "123", MaNhanVien = nv.MaNhanVien, CreatedAt = System.DateTime.Now };
                    _context.TaiKhoans.Add(tk);
                    _context.SaveChanges();
                }
            }
            catch { }

            txtUsername.Focus();
        }

        // ═══════════════════════════════════════
        //  WINDOW CONTROLS
        // ═══════════════════════════════════════

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ═══════════════════════════════════════
        //  INPUT EVENTS
        // ═══════════════════════════════════════

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblUsernamePlaceholder.Visibility = string.IsNullOrEmpty(txtUsername.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            HideMessages();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            bool isEmpty = string.IsNullOrEmpty(txtPassword.Password);
            lblPasswordPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;

            // Keep txtPasswordVisible in sync (without re-triggering)
            txtPasswordVisible.TextChanged -= TxtPasswordVisible_TextChanged;
            txtPasswordVisible.Text = txtPassword.Password;
            txtPasswordVisible.TextChanged += TxtPasswordVisible_TextChanged;

            HideMessages();
        }

        internal void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool isEmpty = string.IsNullOrEmpty(txtPasswordVisible.Text);
            lblPasswordPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;

            // Keep PasswordBox in sync (without re-triggering)
            txtPassword.PasswordChanged -= TxtPassword_PasswordChanged;
            txtPassword.Password = txtPasswordVisible.Text;
            txtPassword.PasswordChanged += TxtPassword_PasswordChanged;

            HideMessages();
        }

        // ═══════════════════════════════════════
        //  TOGGLE SHOW/HIDE PASSWORD
        // ═══════════════════════════════════════

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Focus();
            }
        }

        // ═══════════════════════════════════════
        //  ENTER KEY SUPPORT
        // ═══════════════════════════════════════

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(btnLogin, new RoutedEventArgs());
        }

        // ═══════════════════════════════════════
        //  LOGIN
        // ═══════════════════════════════════════

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Vui lòng nhập tên đăng nhập.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập mật khẩu.");
                if (_isPasswordVisible) txtPasswordVisible.Focus();
                else txtPassword.Focus();
                return;
            }

            btnLogin.IsEnabled = false;
            lblLoginBtnText.Text = "Đang đăng nhập...";

            try
            {
                var account = _context.TaiKhoans
                    .FirstOrDefault(t => t.Username == username && t.PasswordHash == password);

                if (account != null)
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowError("Tên đăng nhập hoặc mật khẩu không chính xác.");
                }
            }
            catch (System.Exception ex)
            {
                ShowError("Lỗi kết nối cơ sở dữ liệu: " + ex.Message);
            }
            finally
            {
                btnLogin.IsEnabled = true;
                lblLoginBtnText.Text = "Đăng nhập";
            }
        }

        // ═══════════════════════════════════════
        //  FORGOT PASSWORD  →  Opens dialog
        // ═══════════════════════════════════════

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            HideMessages();
            var dialog = new ForgotPasswordWindow(_context);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                ShowSuccess("Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập lại.");
            }
        }

        // ═══════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════

        private void ShowError(string message)
        {
            pnlSuccess.Visibility = Visibility.Collapsed;
            lblError.Text = message;
            pnlError.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(string message)
        {
            pnlError.Visibility = Visibility.Collapsed;
            lblSuccess.Text = message;
            pnlSuccess.Visibility = Visibility.Visible;
        }

        private void HideMessages()
        {
            pnlError.Visibility = Visibility.Collapsed;
            pnlSuccess.Visibility = Visibility.Collapsed;
        }
    }
}

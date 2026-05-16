using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.View.Login
{
    public partial class ForgotPasswordWindow : Window
    {
        private readonly QuanLyKhachSanContext _context;
        private TaiKhoan _foundAccount = null;

        public ForgotPasswordWindow(QuanLyKhachSanContext context)
        {
            InitializeComponent();
            _context = context;
            txtFindUsername.Focus();
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
            this.DialogResult = false;
            this.Close();
        }

        // ═══════════════════════════════════════
        //  STEP 1 – FIND ACCOUNT
        // ═══════════════════════════════════════

        private void TxtFindUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblFindUserPlaceholder != null)
            {
                lblFindUserPlaceholder.Visibility = string.IsNullOrEmpty(txtFindUsername.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                lblFindEmailPlaceholder.Visibility = string.IsNullOrEmpty(txtFindEmail.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                lblFindPhonePlaceholder.Visibility = string.IsNullOrEmpty(txtFindPhone.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                lblFindCCCDPlaceholder.Visibility = string.IsNullOrEmpty(txtFindCCCD.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            // Reset found state if user changes the username
            pnlFoundUser.Visibility = Visibility.Collapsed;
            pnlFindError.Visibility = Visibility.Collapsed;
            _foundAccount = null;
        }

        private void TxtFindUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnFind_Click(btnFind, new RoutedEventArgs());
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            string username = txtFindUsername.Text.Trim();
            string email = txtFindEmail.Text.Trim();
            string phone = txtFindPhone.Text.Trim();
            string cccd = txtFindCCCD.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(cccd))
            {
                ShowFindError("Vui lòng nhập đầy đủ thông tin (Tên đăng nhập, Email, SĐT, CCCD).");
                return;
            }

            btnFind.IsEnabled = false;
            btnFind.Content = "Đang tìm...";

            try
            {
                // Include NhanVien to get employee name
                var account = _context.TaiKhoans
                    .Where(t => t.Username == username && 
                                t.NhanVien.Email == email && 
                                t.NhanVien.SoDienThoai == phone && 
                                t.NhanVien.CCCD == cccd)
                    .Select(t => new
                    {
                        TaiKhoan = t,
                        HoTen = t.NhanVien != null ? t.NhanVien.HoTen : "Không xác định",
                        ChucVu = t.NhanVien != null
                            ? (t.NhanVien.ChucVu ? "Quản lý" : "Lễ tân")
                            : "Không xác định"
                    })
                    .FirstOrDefault();

                if (account != null)
                {
                    _foundAccount = account.TaiKhoan;

                    // Show found user info
                    lblFoundName.Text = account.HoTen;
                    lblFoundRole.Text = account.ChucVu;
                    pnlFoundUser.Visibility = Visibility.Visible;
                    pnlFindError.Visibility = Visibility.Collapsed;

                    // Transition to step 2 after a short display
                    btnFind.Content = "Tiếp tục →";
                    btnFind.Click -= BtnFind_Click;
                    btnFind.Click += BtnGoToStep2_Click;
                }
                else
                {
                    _foundAccount = null;
                    pnlFoundUser.Visibility = Visibility.Collapsed;
                    ShowFindError("Không tìm thấy tài khoản hợp lệ với thông tin đã nhập.");
                }
            }
            catch (System.Exception ex)
            {
                ShowFindError("Lỗi kết nối: " + ex.Message);
            }
            finally
            {
                btnFind.IsEnabled = true;
            }
        }

        private void BtnGoToStep2_Click(object sender, RoutedEventArgs e)
        {
            pnlStep1.Visibility = Visibility.Collapsed;
            pnlStep2.Visibility = Visibility.Visible;
            txtNewPassword.Focus();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            pnlStep2.Visibility = Visibility.Collapsed;
            pnlStep1.Visibility = Visibility.Visible;
            pnlResetError.Visibility = Visibility.Collapsed;
            txtNewPassword.Password = "";
            txtConfirmPassword.Password = "";

            // Restore step 1 button state
            btnFind.Content = "Tiếp tục →"; // kept from previous state
            // If user went back, re-enable find flow
            btnFind.Click -= BtnGoToStep2_Click;
            btnFind.Click -= BtnFind_Click;
            btnFind.Click += BtnGoToStep2_Click; // already found, just go to step 2 again
        }

        // ═══════════════════════════════════════
        //  STEP 2 – SET NEW PASSWORD
        // ═══════════════════════════════════════

        private void TxtNewPassword_Changed(object sender, RoutedEventArgs e)
        {
            string pwd = txtNewPassword.Password;
            lblNewPwdPlaceholder.Visibility = string.IsNullOrEmpty(pwd)
                ? Visibility.Visible : Visibility.Collapsed;
            lblConfirmPwdPlaceholder.Visibility = string.IsNullOrEmpty(txtConfirmPassword.Password)
                ? Visibility.Visible : Visibility.Collapsed;

            pnlResetError.Visibility = Visibility.Collapsed;

            // Password strength indicator
            if (string.IsNullOrEmpty(pwd))
            {
                lblStrength.Text = "";
            }
            else if (pwd.Length < 6)
            {
                lblStrength.Text = "⚡ Mật khẩu quá ngắn (tối thiểu 6 ký tự)";
                lblStrength.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            else if (pwd.Length < 10)
            {
                lblStrength.Text = "🔶 Mật khẩu trung bình";
                lblStrength.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                lblStrength.Text = "✅ Mật khẩu mạnh";
                lblStrength.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
        }

        private void TxtNewPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnReset_Click(btnReset, new RoutedEventArgs());
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string newPwd = txtNewPassword.Password;
            string confirmPwd = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(newPwd))
            {
                ShowResetError("Vui lòng nhập mật khẩu mới.");
                txtNewPassword.Focus();
                return;
            }

            if (newPwd.Length < 6)
            {
                ShowResetError("Mật khẩu phải có ít nhất 6 ký tự.");
                txtNewPassword.Focus();
                return;
            }

            if (newPwd != confirmPwd)
            {
                ShowResetError("Mật khẩu xác nhận không khớp. Vui lòng kiểm tra lại.");
                txtConfirmPassword.Focus();
                return;
            }

            if (_foundAccount == null)
            {
                ShowResetError("Không tìm thấy tài khoản. Vui lòng quay lại bước 1.");
                return;
            }

            btnReset.IsEnabled = false;
            btnReset.Content = "Đang cập nhật...";

            try
            {
                // Update password in database
                var accountInDb = _context.TaiKhoans.Find(_foundAccount.MaTaiKhoan);
                if (accountInDb != null)
                {
                    accountInDb.PasswordHash = newPwd;
                    _context.SaveChanges();

                    // Success — close dialog with True result
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowResetError("Tài khoản không còn tồn tại trong hệ thống.");
                }
            }
            catch (System.Exception ex)
            {
                ShowResetError("Lỗi khi lưu dữ liệu: " + ex.Message);
            }
            finally
            {
                btnReset.IsEnabled = true;
                btnReset.Content = "Đặt lại mật khẩu";
            }
        }

        // ═══════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════

        private void ShowFindError(string message)
        {
            pnlFoundUser.Visibility = Visibility.Collapsed;
            lblFindError.Text = message;
            pnlFindError.Visibility = Visibility.Visible;
        }

        private void ShowResetError(string message)
        {
            lblResetError.Text = message;
            pnlResetError.Visibility = Visibility.Visible;
        }
    }
}

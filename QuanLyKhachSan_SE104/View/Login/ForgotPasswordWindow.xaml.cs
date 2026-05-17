using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; 

namespace QuanLyKhachSan_SE104.View.Login
{
    public partial class ForgotPasswordWindow : Window
    {
        private readonly QuanLyKhachSanContext _context;
        private TaiKhoan _foundAccount = null;
        private string _generatedOTP = "";

        // ĐIỀN THÔNG TIN GMAIL CỦA BẠN VÀO ĐÂY
        private readonly string _myEmail = "hotelmanagement.se104@gmail.com";
        private readonly string _myAppPassword = "vhjxbttxojcdaqdy";

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
        // SỰ KIỆN ẨN/HIỆN PLACEHOLDER KHI GÕ
        // ═══════════════════════════════════════
        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblFindUserPlaceholder != null)
                lblFindUserPlaceholder.Visibility = string.IsNullOrEmpty(txtFindUsername.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (lblFindEmailPlaceholder != null)
                lblFindEmailPlaceholder.Visibility = string.IsNullOrEmpty(txtFindEmail.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (lblOTPPlaceholder != null)
                lblOTPPlaceholder.Visibility = string.IsNullOrEmpty(txtOTP.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (pnlFindError != null) pnlFindError.Visibility = Visibility.Collapsed;
        }

        // ═══════════════════════════════════════
        // STEP 1: GỬI OTP
        // ═══════════════════════════════════════
        private async void BtnSendOTP_Click(object sender, RoutedEventArgs e)
        {
            string username = txtFindUsername.Text.Trim();
            string email = txtFindEmail.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                ShowFindError("Vui lòng nhập Username và Email."); return;
            }

            btnFind.IsEnabled = false;
            btnFind.Content = "Đang kiểm tra & Gửi mã...";

            try
            {
                var account = await _context.TaiKhoans
                    .Include(t => t.NhanVien)
                    .FirstOrDefaultAsync(t => t.Username == username && t.NhanVien.Email == email);

                if (account != null)
                {
                    _foundAccount = account;
                    _generatedOTP = new Random().Next(100000, 999999).ToString();

                    await System.Threading.Tasks.Task.Run(() => SendEmailOTP(email, _generatedOTP));

                    pnlStep1.Visibility = Visibility.Collapsed;
                    pnlStep2.Visibility = Visibility.Visible;
                    txtOTP.Focus();
                }
                else
                {
                    ShowFindError("Username hoặc Email không tồn tại trong hệ thống.");
                }
            }
            catch (Exception ex)
            {
                ShowFindError("Lỗi hệ thống: " + ex.Message);
            }
            finally
            {
                btnFind.IsEnabled = true;
                btnFind.Content = "Gửi mã OTP";
            }
        }

        private void SendEmailOTP(string toEmail, string otpCode)
        {
            var fromAddress = new MailAddress(_myEmail, "Hotel Manager System");
            var toAddress = new MailAddress(toEmail);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _myAppPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Mã xác thực cấp lại mật khẩu - Hotel Manager",
                Body = $"Xin chào,\n\nMã xác thực OTP của bạn là: {otpCode}\n\nVui lòng không chia sẻ mã này cho bất kỳ ai.\nMã có hiệu lực trong 5 phút."
            })
            {
                smtp.Send(message);
            }
        }

        // ═══════════════════════════════════════
        // STEP 2: XÁC NHẬN OTP
        // ═══════════════════════════════════════
        private void BtnVerifyOTP_Click(object sender, RoutedEventArgs e)
        {
            string inputOTP = txtOTP.Text.Trim();

            if (inputOTP == _generatedOTP)
            {
                pnlStep2.Visibility = Visibility.Collapsed;
                pnlStep3.Visibility = Visibility.Visible;
                txtNewPassword.Focus();
            }
            else
            {
                MessageBox.Show("Mã OTP không chính xác!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════
        // STEP 3: ĐẶT MẬT KHẨU MỚI
        // ═══════════════════════════════════════
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            pnlStep3.Visibility = Visibility.Collapsed;
            pnlStep2.Visibility = Visibility.Collapsed;
            pnlStep1.Visibility = Visibility.Visible;

            txtOTP.Text = "";
            txtNewPassword.Password = "";
            txtConfirmPassword.Password = "";
            _generatedOTP = "";
            _foundAccount = null;
        }

        private void TxtNewPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (txtNewPassword == null || lblNewPwdPlaceholder == null) return;
            string pwd = txtNewPassword.Password;
            lblNewPwdPlaceholder.Visibility = string.IsNullOrEmpty(pwd) ? Visibility.Visible : Visibility.Collapsed;

            if (txtConfirmPassword != null && lblConfirmPwdPlaceholder != null)
            {
                lblConfirmPwdPlaceholder.Visibility = string.IsNullOrEmpty(txtConfirmPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (pnlResetError != null) pnlResetError.Visibility = Visibility.Collapsed;

            if (lblStrength == null) return;
            if (string.IsNullOrEmpty(pwd))
            {
                lblStrength.Text = "";
            }
            else if (pwd.Length < 6)
            {
                lblStrength.Text = "⚡ Mật khẩu quá ngắn (tối thiểu 6 ký tự)";
                lblStrength.Foreground = Brushes.OrangeRed;
            }
            else if (pwd.Length < 10)
            {
                lblStrength.Text = "🔶 Mật khẩu trung bình";
                lblStrength.Foreground = Brushes.Orange;
            }
            else
            {
                lblStrength.Text = "✅ Mật khẩu mạnh";
                lblStrength.Foreground = Brushes.LightGreen;
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
                var accountInDb = _context.TaiKhoans.Find(_foundAccount.MaTaiKhoan);
                if (accountInDb != null)
                {
                    accountInDb.PasswordHash = newPwd;
                    _context.SaveChanges();

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowResetError("Tài khoản không còn tồn tại trong hệ thống.");
                }
            }
            catch (Exception ex)
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
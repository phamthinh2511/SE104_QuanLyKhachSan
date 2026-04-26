using QuanLyKhachSan_SE104.Model;
using System;
using System.Linq;
using System.Windows;

namespace QuanLyKhachSan_SE104.View.KhachHang
{
    public partial class KhachHangDialog : Window
    {
        private Model.KhachHang _khachHang;
        private bool _isEdit = false;

        public KhachHangDialog()
        {
            InitializeComponent();
            _isEdit = false;
            TitleText.Text = "👤 Thêm khách hàng mới";
        }

        public KhachHangDialog(Model.KhachHang kh) : this()
        {
            _khachHang = kh;
            _isEdit = true;
            TitleText.Text = "👤 Cập nhật khách hàng";
            LoadData();
        }

        private void LoadData()
        {
            if (_khachHang == null) return;
            txtName.Text = _khachHang.HoTen;
            cbGender.SelectedIndex = _khachHang.GioiTinh ? 1 : 0;
            cbNationality.Text = _khachHang.QuocTich;
            txtIDCard.Text = _khachHang.CCCD_Passport;
            txtPhone.Text = _khachHang.SDT;
            txtAddress.Text = _khachHang.DiaChi;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtIDCard.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Họ tên, CCCD và Số điện thoại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new QuanLyKhachSanContext())
                {
                    if (_isEdit)
                    {
                        var kh = db.KhachHangs.Find(_khachHang.MaKhachHang);
                        if (kh != null)
                        {
                            kh.HoTen = txtName.Text;
                            kh.GioiTinh = cbGender.SelectedIndex == 1;
                            kh.QuocTich = cbNationality.Text;
                            kh.CCCD_Passport = txtIDCard.Text;
                            kh.SDT = txtPhone.Text;
                            kh.DiaChi = txtAddress.Text;
                        }
                    }
                    else
                    {
                        // Kiểm tra CCCD trùng
                        if (db.KhachHangs.Any(k => k.CCCD_Passport == txtIDCard.Text))
                        {
                            MessageBox.Show("Số CCCD/Passport này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var kh = new Model.KhachHang
                        {
                            HoTen = txtName.Text,
                            GioiTinh = cbGender.SelectedIndex == 1,
                            QuocTich = cbNationality.Text,
                            CCCD_Passport = txtIDCard.Text,
                            SDT = txtPhone.Text,
                            DiaChi = txtAddress.Text
                        };
                        db.KhachHangs.Add(kh);
                    }
                    db.SaveChanges();
                    MessageBox.Show(_isEdit ? "Cập nhật thành công!" : "Thêm khách hàng thành công!");
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message);
            }
        }
    }
}

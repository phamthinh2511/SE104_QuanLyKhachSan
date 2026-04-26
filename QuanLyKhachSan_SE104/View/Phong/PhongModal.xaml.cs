using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.ViewModel.PhongVM;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System;

// ĐẶT BIỆT DANH (ALIAS) Ở ĐÂY ĐỂ TRÁNH XUNG ĐỘT
using ModelPhong = QuanLyKhachSan_SE104.Model.Phong;

namespace QuanLyKhachSan_SE104.View.PhongView
{
    /// <summary>
    /// Interaction logic for PhongModal.xaml
    /// </summary>
    public partial class PhongModal : Window
    {
        // Sử dụng ModelPhong thay cho Phong
        public ModelPhong PhongInfo { get; set; }
        public List<LoaiPhong> DanhSachLoaiPhong { get; set; }
        private ModelPhong _originPhong;

        public PhongModal(ModelPhong p = null)
        {
            InitializeComponent();

            // Load danh sách loại phòng từ DataBase
            using (var context = new QuanLyKhachSanContext())
            {
                this.DanhSachLoaiPhong = context.LoaiPhongs.AsNoTracking().ToList();
            }

            // Khởi tạo
            if (p == null)
            {
                this.PhongInfo = new ModelPhong { TrangThai = 0, TrangThaiDonDep = 0 }; // Giá trị mặc định
                _originPhong = null;
            }
            else
            {
                this.PhongInfo = p;
                // Clone object để so sánh thay đổi
                _originPhong = new ModelPhong
                {
                    MaPhong = p.MaPhong,
                    TenPhong = p.TenPhong,
                    MaLoaiPhong = p.MaLoaiPhong,
                    SoTang = p.SoTang,
                    TrangThai = p.TrangThai,
                    TrangThaiDonDep = p.TrangThaiDonDep
                };
            }

            // Kết nối DataContext
            this.DataContext = new PhongModalViewModel(this.PhongInfo, this.DanhSachLoaiPhong);
        }

        private bool IsDataChanged()
        {
            // Nếu là thêm mới và đã nhập tên thì coi như có thay đổi
            var vm = this.DataContext as PhongModalViewModel;
            if (vm == null) return false;

            if (_originPhong == null)
                return !string.IsNullOrWhiteSpace(vm.TenPhong);

            // So sánh dữ liệu cũ và mới
            return vm.TenPhong?.Trim() != _originPhong.TenPhong?.Trim() ||
                   vm.SoTang != _originPhong.SoTang ||
                   vm.LoaiPhong?.MaLoaiPhong != _originPhong.MaLoaiPhong ||
                   vm.TrangThaiValue != _originPhong.TrangThai ||
                   vm.TrangThaiDonDepValue != _originPhong.TrangThaiDonDep;
        }

        private void button_Save(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as PhongModalViewModel;
                if (vm == null || string.IsNullOrWhiteSpace(vm.TenPhong) || vm.LoaiPhong == null)
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ tên phòng và loại phòng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new QuanLyKhachSanContext())
                {
                    if (_originPhong == null)
                    {
                        // THÊM MỚI (Dùng ModelPhong)
                        var newPhong = new ModelPhong
                        {
                            TenPhong = vm.TenPhong.Trim(),
                            MaLoaiPhong = vm.LoaiPhong.MaLoaiPhong,
                            SoTang = vm.SoTang,
                            TrangThai = vm.TrangThaiValue,
                            TrangThaiDonDep = vm.TrangThaiDonDepValue
                        };
                        context.Phongs.Add(newPhong);
                    }
                    else
                    {
                        // CẬP NHẬT
                        var updatePhong = context.Phongs.FirstOrDefault(p => p.MaPhong == vm.MaPhong);
                        if (updatePhong != null)
                        {
                            updatePhong.TenPhong = vm.TenPhong.Trim();
                            updatePhong.MaLoaiPhong = vm.LoaiPhong.MaLoaiPhong;
                            updatePhong.SoTang = vm.SoTang;
                            updatePhong.TrangThai = vm.TrangThaiValue;
                            updatePhong.TrangThaiDonDep = vm.TrangThaiDonDepValue;

                            // Ép EF Core ghi nhận object này đã bị chỉnh sửa
                            context.Phongs.Update(updatePhong);
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy phòng trong CSDL để sửa!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Kiểm tra xem có bao nhiêu dòng dữ liệu bị thay đổi trong DB
                    int changes = context.SaveChanges();
                    if (changes == 0)
                    {
                        MessageBox.Show("Không có dữ liệu mới nào được lưu. Có thể bạn chưa thay đổi gì so với ban đầu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Lưu thông tin thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void button_Cancel(object sender, RoutedEventArgs e)
        {
            if (!IsDataChanged())
            {
                this.Close();
                return;
            }

            var result = MessageBox.Show("Bạn có muốn hủy bỏ các thay đổi và đóng cửa sổ?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void button_Close(object sender, RoutedEventArgs e) => this.Close();
    }
}
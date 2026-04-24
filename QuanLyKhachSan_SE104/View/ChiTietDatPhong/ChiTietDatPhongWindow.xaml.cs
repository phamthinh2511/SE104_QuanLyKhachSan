using QuanLyKhachSan_SE104.DTO; // Thêm using này
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhong;

namespace QuanLyKhachSan_SE104.View.ChiTietDatPhong
{
    public partial class ChiTietDatPhongWindow : Window
    {
        // Nhận DTO thay vì Model
        public ChiTietDatPhongWindow(DatPhongDTO datPhongDto)
        {
            InitializeComponent();
            // Khởi tạo VM với DTO
            this.DataContext = new ChiTietDatPhongWindowViewModel(datPhongDto);

            // Đăng ký sự kiện đóng cửa sổ từ VM (nếu VM có RequestClose)
            if (this.DataContext is ChiTietDatPhongWindowViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        }
    }
}
using QuanLyKhachSan_SE104.DTO; // Thêm using này
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM;

namespace QuanLyKhachSan_SE104.View.ChiTietDatPhongView
{
    public partial class ChiTietDatPhongWindow : Window
    {
        // Nhận DTO thay vì Model
        public ChiTietDatPhongWindow(ChiTietDatPhongWindowViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
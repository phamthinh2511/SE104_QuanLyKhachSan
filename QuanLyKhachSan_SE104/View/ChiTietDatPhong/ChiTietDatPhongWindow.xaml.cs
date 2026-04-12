using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using QuanLyKhachSan_SE104.Utilities;

// Dùng alias để tránh conflict tên DatPhong
using ModelDatPhong = QuanLyKhachSan_SE104.Model.DatPhong;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.View.ChiTietDatPhong
{
    public partial class ChiTietDatPhongWindow : Window
    {
        public ChiTietDatPhongWindow(ModelDatPhong datPhong)
        {
            InitializeComponent();
            this.DataContext = new ChiTietDatPhongWindowVM(datPhong, () => this.Close());
        }
    }

    public class ChiTietPhongRow
    {
        public string TenPhong { get; set; }
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public int SoNguoi { get; set; }
    }

    public class ChiTietDatPhongWindowVM
    {
        public string TieuDe { get; }
        public string TenKhachHang { get; }
        public string NgayDat { get; }
        public string TenNhanVien { get; }
        public ObservableCollection<ChiTietPhongRow> DanhSachChiTiet { get; }
        public System.Windows.Input.ICommand ThoatCommand { get; }

        public ChiTietDatPhongWindowVM(ModelDatPhong dp, Action closeAction)
        {
            TieuDe = $"Chi Tiết Phiếu Thuê {dp.MaDatPhong}";
            TenKhachHang = dp.KhachHang?.HoTen ?? "—";
            NgayDat = dp.NgayDat.ToString("MM/dd/yyyy hh:mm:ss tt");
            TenNhanVien = dp.NhanVien?.HoTen ?? "—";

            DanhSachChiTiet = new ObservableCollection<ChiTietPhongRow>(
                dp.ChiTietDatPhongs?.Select(ct => new ChiTietPhongRow
                {
                    TenPhong = $"P{ct.MaPhong:000}",
                    NgayCheckIn = ct.NgayCheckIn,
                    NgayCheckOut = ct.NgayCheckOut,
                    SoNguoi = 1
                }) ?? Enumerable.Empty<ChiTietPhongRow>()
            );

            ThoatCommand = new RelayCommand<object>(_ => closeAction?.Invoke());
        }
    }
}
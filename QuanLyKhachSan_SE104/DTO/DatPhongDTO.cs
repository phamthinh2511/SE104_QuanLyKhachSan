using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyKhachSan_SE104.DTO
{
    // Used by ChiTietDatPhongPage.xaml (ListDatPhong) and ChiTietDatPhongWindow.xaml (DataContext)
    public class DatPhongDTO
    {
        public int MaDatPhong { get; set; }

        // Binding: ChiTietDatPhongWindow.xaml -> TieuDe
        public string TieuDe => $"PHIẾU ĐẶT PHÒNG #{MaDatPhong}";

        // Binding: ChiTietDatPhongPage.xaml -> NgayDat (StringFormat='dd/MM/yyyy')
        // Binding: ChiTietDatPhongWindow.xaml -> NgayDat
        public DateTime NgayDat { get; set; }

        public int TrangThaiDat { get; set; }
        public decimal TienCoc { get; set; }

        // Binding: ChiTietDatPhongPage.xaml -> KhachHang.HoTen
        // Binding: ChiTietDatPhongWindow.xaml -> TenKhachHang
        public string TenKhachHang { get; set; }

        public string SDT { get; set; }

        // Binding: ChiTietDatPhongPage.xaml -> NhanVien.HoTen
        // Binding: ChiTietDatPhongWindow.xaml -> TenNhanVien
        public string TenNhanVien { get; set; }

        // Binding: ChiTietDatPhongWindow.xaml -> DanhSachChiTiet (ItemsControl)
        public List<ChiTietPhongDTO> DanhSachChiTiet { get; set; } = new List<ChiTietPhongDTO>();
    }

    // Used by ChiTietDatPhongWindow.xaml -> DanhSachChiTiet DataTemplate
    public class ChiTietPhongDTO
    {
        public int MaChiTietDatPhong { get; set; }

        // Binding: TenPhong
        public string TenPhong { get; set; }

        // Binding: NgayCheckIn (StringFormat='dd/MM/yyyy HH:mm')
        public DateTime NgayCheckIn { get; set; }

        // Binding: NgayCheckOut (StringFormat='dd/MM/yyyy HH:mm')
        public DateTime NgayCheckOut { get; set; }

        // Binding: SoNguoi
        public int SoNguoi { get; set; }
    }

    // Used by DatPhongPage.xaml -> SelectedRoomsList DataTemplate
    public class SelectedRoomItem
    {
        // Binding: RoomName
        public string RoomName { get; set; }

        // Binding: Capacity (TwoWay)
        private int _capacity = 1; // Mặc định ban đầu là 1 người
        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                OnPropertyChanged();
            }
        }

        public int MaPhong { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
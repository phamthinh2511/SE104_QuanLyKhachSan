using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DTO
{
    public class HoaDonChiTietDTO
    {
        public string MaHoaDonFormatted { get; set; } // Hiện dạng #HD-000001
        public int MaDatPhong { get; set; }
        public string TenKhachHang { get; set; }
        public string SDT { get; set; }
        public string TenPhong { get; set; }
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public int SoDem { get; set; }
        public string TenNhanVien { get; set; }

        public List<DichVuDaDungDTO> DanhSachDichVu { get; set; } = new List<DichVuDaDungDTO>();

        // Tổng hợp chi phí
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TienCoc { get; set; }
        public decimal TongThanhToan { get; set; }
    }
    public class DichVuDaDungDTO
    {
        public string TenDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; } // SoLuong * DonGia
    }
}

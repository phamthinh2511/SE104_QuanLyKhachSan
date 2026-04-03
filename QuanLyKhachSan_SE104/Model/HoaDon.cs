using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class HoaDon
    {
        [Key]
        public int MaHoaDon { get; set; }
        public int MaDatPhong { get; set; }
        public int MaNhanVien { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public decimal PhuPhi { get; set; }
        public decimal TienCoc { get; set; }
        public decimal TongThanhToan { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public int PhuongThucThanhToan { get; set; } // 0=Tiền mặt, 1=Thẻ, 2=Chuyển khoản
        public string GhiChu { get; set; }
        public string TrangThaiThanhToan { get; set; }

        public DatPhong DatPhong { get; set; }
        public NhanVien NhanVien { get; set; }
    }
}

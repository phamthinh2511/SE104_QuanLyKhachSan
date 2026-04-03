using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class DatPhong
    {
        [Key]
        public int MaDatPhong { get; set; }
        public int MaKhachHang { get; set; }
        public int TrangThaiDat { get; set; } //0 = Chờ xác nhận, 1 = Đã xác nhận, 2 = Đã nhận phòng, 3 = Đã trả phòng, 4 = Đã hủy
        public int MaNhanVien { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TienCoc { get; set; }

        public KhachHang KhachHang { get; set; }
        public NhanVien NhanVien { get; set; }

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public HoaDon HoaDon { get; set; }
    }
}

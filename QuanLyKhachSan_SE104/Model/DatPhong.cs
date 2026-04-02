using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class DatPhong
    {
        [Key]
        public int MaDatPhong { get; set; }
        public int MaKhachHang { get; set; }
        public string TrangThaiDat { get; set; }
        public int MaNhanVien { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TienCoc { get; set; }
    }
}

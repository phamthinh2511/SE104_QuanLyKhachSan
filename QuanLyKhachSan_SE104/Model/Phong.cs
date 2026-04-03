using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }
        public string TenPhong { get; set; }
        public int MaLoaiPhong { get; set; }
        public int SoTang { get; set; }
        public int TrangThaiThue { get; set; } // 0: Trống, 1: Đã đặt, 2: Đang ở
        public int TrangThaiDonDep { get; set; } // 0: Sạch, 1: Đang dọn, 2: Cần dọn, 3: Bảo trì

        public LoaiPhong LoaiPhong { get; set; }
        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
    }
}

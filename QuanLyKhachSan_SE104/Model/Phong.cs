using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    /// <summary>
    /// TrangThai (trạng thái thuê):
    ///   0 = Trống
    ///   1 = Đã đặt (có booking nhưng chưa check-in)
    ///   2 = Đang ở
    ///   3 = Quá hạn
    ///   4 = Cần dọn dẹp
    ///   5 = Bảo trì
    /// TrangThaiDonDep:
    ///   0 = Sạch, 1 = Đang dọn, 2 = Cần dọn, 3 = Bảo trì
    /// </summary>
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }
        public string TenPhong { get; set; }
        public int MaLoaiPhong { get; set; }
        public int SoTang { get; set; }

        // Alias để XAML binding nhất quán với tên property trong DataTrigger
        public int TrangThai { get; set; }
        public int TrangThaiThue => TrangThai; // Alias tương thích với XAML cũ

        public int TrangThaiDonDep { get; set; }
        public LoaiPhong LoaiPhong { get; set; }
        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
    }
}
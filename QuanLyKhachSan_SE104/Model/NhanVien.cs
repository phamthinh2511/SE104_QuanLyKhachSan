using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class NhanVien
    {
        [Key]
        public int MaNhanVien { get; set; }
        public string HoTen { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? CCCD { get; set; }
        public bool ChucVu { get; set; } // 0 = Lễ tân, 1 = Quản lý
        public bool TrangThaiLamViec { get; set; } // 1 = Đang làm, 0 = Nghỉ việc

        public ICollection<TaiKhoan> TaiKhoans { get; set; }
        public ICollection<HoaDon> HoaDons { get; set; }
        public ICollection<DatPhong> DatPhongs { get; set; }
    }
}

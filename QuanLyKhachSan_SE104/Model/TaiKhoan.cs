using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class TaiKhoan
    {
            [Key]
            public int MaTaiKhoan { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string HoTen { get; set; }
            public string ChucVu { get; set; } // 0 = Lễ tân, 1 = Quản lý
            public bool TrangThaiLamViec { get; set; } // 1 = Đang làm, 0 = Nghỉ việc
            public DateTime CreatedAt { get; set; }
    }
}

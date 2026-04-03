using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class TaiKhoan
    {
            [Key]
            public int MaTaiKhoan { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public DateTime CreatedAt { get; set; }

            public int MaNhanVien { get; set; }
            public NhanVien NhanVien { get; set; }
    }
}

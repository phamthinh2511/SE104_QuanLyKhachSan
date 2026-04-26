using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class KhachHang
    {
        [Key]
        public int MaKhachHang { get; set; }
        public string HoTen { get; set; }
        public string GioiTinh { get; set; }
        public string QuocTich { get; set; }
        public string CCCD_Passport { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<DatPhong> DatPhongs { get; set; }
    }
}

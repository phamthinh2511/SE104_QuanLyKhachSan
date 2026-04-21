using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class ChiTietDatPhong
    {
        [Key]
        public int MaChiTietDatPhong { get; set; }
        public int MaDatPhong { get; set; }
        public int MaPhong { get; set; }
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public decimal GiaDat { get; set; }

        public DatPhong DatPhong { get; set; }
        public Phong Phong { get; set; }

        public int SoNguoi { get; set; }

        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
    }
}

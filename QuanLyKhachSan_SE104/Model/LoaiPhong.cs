using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class LoaiPhong
    {
        [Key]
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public decimal GiaMacDinh { get; set; }
        public int SoGiuong { get; set; }
        public int SoNguoiToiDa { get; set; }
        public decimal PhuPhiThemGio { get; set; }

        public ICollection<Phong> Phongs { get; set; }
    }
}

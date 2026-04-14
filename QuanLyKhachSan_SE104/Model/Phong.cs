using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan_SE104.Model
{
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }

        public string TenPhong { get; set; }

        public int MaLoaiPhong { get; set; }

        public int SoTang { get; set; }

        // Thêm [Column] nếu bạn chưa chạy Migration để đổi tên cột trong Database
        [Column("TrangThaiThue")]
        public int TrangThai { get; set; }

        public int TrangThaiDonDep { get; set; }

        public LoaiPhong LoaiPhong { get; set; }

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
    }
}
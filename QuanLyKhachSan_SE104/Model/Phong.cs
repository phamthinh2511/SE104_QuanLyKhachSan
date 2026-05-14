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

        public bool IsDeleted { get; set; } = false;

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }

        public bool IsCheckInToday =>
    ChiTietDatPhongs != null &&
    ChiTietDatPhongs.Any(ct =>
        ct.NgayCheckIn.Date == DateTime.Today &&
        ct.DatPhong != null &&
        ct.DatPhong.TrangThaiDat != 3 &&  // bỏ qua đã trả phòng
        ct.DatPhong.TrangThaiDat != 4);   // bỏ qua đã hủy

        public bool IsCheckOutToday =>
            ChiTietDatPhongs != null &&
            ChiTietDatPhongs.Any(ct =>
                ct.NgayCheckOut.Date == DateTime.Today &&
                ct.DatPhong != null &&
                ct.DatPhong.TrangThaiDat != 3 &&
                ct.DatPhong.TrangThaiDat != 4);
    }
}
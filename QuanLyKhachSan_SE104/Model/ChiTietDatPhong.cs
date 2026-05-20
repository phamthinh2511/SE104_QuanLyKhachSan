using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public enum TrangThaiSegment
    {
        ChoNhanPhong = 0, // Booked - Khách chưa tới
        DangO = 1,        // Active - Khách đang ở phòng này
        DaDoiPhong = 2,   // Terminated/Archived - Segment cũ đã đóng do đổi phòng
        DaCheckOut = 3    // CheckedOut - Đã checkout bình thường
    }
    public class ChiTietDatPhong
    {
        [Key]
        public int MaChiTietDatPhong { get; set; }
        public int MaDatPhong { get; set; }
        public int MaPhong { get; set; }
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public decimal GiaDat { get; set; }
        public int SoDem { get; set; }
        public decimal ThanhTien { get; set; }
        public int SoNguoi { get; set; }
        public TrangThaiSegment TrangThaiSegment { get; set; }

        public DatPhong DatPhong { get; set; }
        public Phong Phong { get; set; }

        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
    }
}

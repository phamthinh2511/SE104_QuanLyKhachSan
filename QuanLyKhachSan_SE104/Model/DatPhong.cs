using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class DatPhong
    {
        [Key]
        public int MaDatPhong { get; set; }
        public int MaKhachHang { get; set; }
        /// <summary>
        /// 0 = Chờ xác nhận
        /// 1 = Đã xác nhận
        /// 2 = Đã nhận phòng
        /// 3 = Đã trả phòng
        /// 4 = Đã hủy (timely — hoàn cọc)
        /// 5 = No-show (forfeit cọc)
        /// </summary>
        public int TrangThaiDat { get; set; } 
        public int MaNhanVien { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TienCoc { get; set; }

        /// <summary>
        /// 0 = Đang giữ (chưa xử lý)
        /// 1 = Đã hoàn trả (Rule 01 - hủy đúng hạn)
        /// 2 = Đã thu vào doanh thu (Rule 02 - no-show / checkout)
        /// 3 = Đã chuyển booking khác (Rule 04 - đổi loại phòng)
        /// </summary>
        public int TrangThaiCoc { get; set; } = 0;

        public KhachHang KhachHang { get; set; }
        public NhanVien NhanVien { get; set; }

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public HoaDon HoaDon { get; set; }
        public ICollection<LichSuCoc> LichSuCocs { get; set; }
    }
}

namespace QuanLyKhachSan_SE104.DTOs
{
    public class DoiPhongRequestDTO
    {
        public int MaDatPhong { get; set; }
        public int MaChiTietDatPhong { get; set; }
        public int MaPhongCu { get; set; }
        public int MaPhongMoi { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public int MaNhanVien { get; set; }
    }
}

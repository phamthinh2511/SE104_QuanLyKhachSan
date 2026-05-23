namespace QuanLyKhachSan_SE104.DTO
{
    public class CheckOutRequestDTO
    {
        public int MaDatPhong { get; set; }
        public int MaChiTietDatPhongActive { get; set; }
        public int MaNhanVien { get; set; }
        public int PhuongThucThanhToan { get; set; }
        public string GhiChu { get; set; } = "";
    }
}

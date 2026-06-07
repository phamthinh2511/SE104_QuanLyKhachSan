namespace QuanLyKhachSan_SE104.DTOs
{
    public class BookingRequestDTO
    {
        public List<int> MaPhongList { get; set; } = new();
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string SDT { get; set; } = string.Empty;
        public string CCCD { get; set; } = string.Empty;
        public string GioiTinh { get; set; } = string.Empty;
        public string QuocTich { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public decimal TienCoc { get; set; }
        public bool IsWalkIn { get; set; }
        public int MaNhanVien { get; set; }
        public Dictionary<int, int> SoNguoiPerRoom { get; set; } = new();
    }
}
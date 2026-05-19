namespace QuanLyKhachSan_SE104.DTOs
{
    public class PhongDisplayDTO
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; } = string.Empty;
        public decimal GiaMacDinh { get; set; }
        public int SoTang { get; set; }
        public int TrangThai { get; set; }
        public int TrangThaiDonDep { get; set; }
        public bool IsCheckInToday { get; set; }
        public bool IsCheckOutToday { get; set; }
        public bool IsAlertActive => IsCheckInToday || IsCheckOutToday;
    }
}

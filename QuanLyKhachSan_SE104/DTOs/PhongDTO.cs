namespace QuanLyKhachSan_SE104.DTOs
{
    public class PhongDTO
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public int SoTang { get; set; }
        public decimal GiaMacDinh { get; set; }
        public int TrangThaiDonDep { get; set; }
        public string TenLoaiPhong { get; set; } = string.Empty;
        public int SoNguoiToiDa { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class ChiTietDichVu
    {
        [Key]
        public int MaChiTietDV { get; set; }
        public int MaChiTietDatPhong { get; set; }
        public int MaDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public DateTime ThoiGianSuDung { get; set; }
        public decimal ThanhTien => DonGia * SoLuong; // thêm dòng này
        public ChiTietDatPhong ChiTietDatPhong { get; set; }
        public DichVu DichVu { get; set; }
    }
}

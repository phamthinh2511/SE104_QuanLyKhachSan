using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }
        public int LoaiDichVu { get; set; } // 0=Thức ăn, 1=Đồ uống, 2=Giặt ủi...
        public string TenDichVu { get; set; }
        public decimal DonGia { get; set; }
        public string MoTa { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
    }
}

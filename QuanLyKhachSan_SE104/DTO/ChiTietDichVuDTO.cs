using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DTO
{
    public class ChiTietDichVuDTO
    {
        public string TenDichVu { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        public decimal ThanhTien => DonGia * SoLuong;
    }
}




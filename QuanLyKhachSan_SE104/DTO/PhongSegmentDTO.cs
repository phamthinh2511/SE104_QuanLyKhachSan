using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DTO
{
    public class PhongSegmentDTO
    {
        public string TenPhong { get; set; }
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public int SoDem { get; set; }
        public decimal GiaMoiDem { get; set; }
        public decimal ThanhTien => GiaMoiDem * SoDem;
        public bool IsCurrentRoom { get; set; }   // true for the room the guest is currently in
        public int SoGioQuaHan { get; set; }
        public decimal PhuPhiQuaHan { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_SE104.DTO
{
    public class InvoiceDetailDTO
    {
        public int MaDatPhong { get; set; }
        public int MaChiTietDatPhongActive { get; set; }

        public string MaHoaDonText { get; set; } = "Chưa lập";
        public string TenKhachHang { get; set; } = "";
        public string SdtKhachHang { get; set; } = "";
        public string TenNhanVien { get; set; } = "";
        public string TenPhong { get; set; } = "";

        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public DateTime NgayCheckOutHopDong { get; set; }

        public List<PhongSegmentDTO> DanhSachSegment { get; set; } = new();
        public List<ChiTietDichVuDTO> DanhSachDichVu { get; set; } = new();

        public int SoGioQuaHan { get; set; }
        public decimal PhuPhiMoiGio { get; set; }

        public decimal TongTienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public decimal PhuPhi { get; set; }
        public decimal TienCoc { get; set; }
        public bool DepositAlreadyApplied { get; set; }
        public decimal TongThanhToan { get; set; }

        public bool IsPaid { get; set; }
        public string TrangThaiThanhToanText { get; set; } = "Chờ thanh toán";
        public int PhuongThucThanhToan { get; set; } = -1;
        public string PhuongThucThanhToanText { get; set; } = "";
        public DateTime? NgayThanhToan { get; set; }
        public string NgayThanhToanText { get; set; } = "";

        public string GhiChu { get; set; } = "";
    }
}

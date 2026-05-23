using QuanLyKhachSan_SE104.DTO;

namespace QuanLyKhachSan_SE104.Services
{
    public interface IHoaDonService
    {
        InvoiceDetailDTO GetInvoiceDetails(int maDatPhong, int maChiTietDatPhong, int maNhanVienCheckout);
        InvoiceDetailDTO ProcessCheckOut(CheckOutRequestDTO request);
    }
}

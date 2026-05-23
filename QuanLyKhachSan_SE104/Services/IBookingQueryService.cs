using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public interface IBookingQueryService
    {
        IReadOnlyList<PhongDisplayDTO> GetAllRoomsForDisplay();
        ChiTietDatPhong? GetActiveRoomDetail(int maPhong);
        ChiTietDatPhong? GetRoomDetailBySegment(int maPhong, TrangThaiSegment segmentStatus);
    }
}

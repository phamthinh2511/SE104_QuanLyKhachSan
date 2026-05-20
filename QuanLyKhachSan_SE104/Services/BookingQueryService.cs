using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public class BookingQueryService : IBookingQueryService
    {
        private static readonly TimeSpan CheckoutDeadline = TimeSpan.FromHours(12); // 12:00 PM
        public IReadOnlyList<PhongDisplayDTO> GetAllRoomsForDisplay()
        {
            using var ctx = new QuanLyKhachSanContext();
            var today = DateTime.Today;
            var now = DateTime.Now;

            var rooms = ctx.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.DatPhong)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.TenPhong)
                .ToList();

            return rooms.Select(room =>
            {
                var activeSegments = room.ChiTietDatPhongs?
                    .Where(ct => ct.TrangThaiSegment == TrangThaiSegment.ChoNhanPhong
                              || ct.TrangThaiSegment == TrangThaiSegment.DangO)
                    .ToList();

                var isCheckInToday = room.TrangThai == 1
                    && (activeSegments?.Any(ct =>
                        ct.TrangThaiSegment == TrangThaiSegment.ChoNhanPhong &&
                        ct.NgayCheckIn.Date == today) ?? false);

                var checkoutDeadlineToday = today + CheckoutDeadline; // so sanh gio check out voi hnay + 12 tieng

                var isCheckOutToday = (room.TrangThai == 2 || room.TrangThai == 3)
                    && now < checkoutDeadlineToday
                    && (activeSegments?.Any(ct =>
                        ct.TrangThaiSegment == TrangThaiSegment.DangO &&
                        ct.NgayCheckOut.Date == today) ?? false);

                return new PhongDisplayDTO
                {
                    MaPhong = room.MaPhong,
                    TenPhong = room.TenPhong,
                    MaLoaiPhong = room.MaLoaiPhong,
                    TenLoaiPhong = room.LoaiPhong?.TenLoaiPhong ?? string.Empty,
                    GiaMacDinh = room.LoaiPhong?.GiaMacDinh ?? 0,
                    SoTang = room.SoTang,
                    TrangThai = room.TrangThai,
                    TrangThaiDonDep = room.TrangThaiDonDep,
                    IsCheckInToday = isCheckInToday,
                    IsCheckOutToday = isCheckOutToday
                };
            }).ToList();
        }

        public ChiTietDatPhong? GetActiveRoomDetail(int maPhong)
            => GetRoomDetailBySegment(maPhong, TrangThaiSegment.DangO);

        public ChiTietDatPhong? GetRoomDetailBySegment(int maPhong, TrangThaiSegment segmentStatus)
        {
            using var ctx = new QuanLyKhachSanContext();

            return ctx.ChiTietDatPhongs
                .AsNoTracking()
                .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                .Include(c => c.ChiTietDichVus).ThenInclude(dv => dv.DichVu)
                .Where(c => c.MaPhong == maPhong
                    && c.TrangThaiSegment == segmentStatus)
                .OrderByDescending(c => c.MaChiTietDatPhong)
                .FirstOrDefault();
        }
    }
}

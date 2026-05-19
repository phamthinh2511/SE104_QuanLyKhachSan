using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public class BookingQueryService : IBookingQueryService
    {
        public IReadOnlyList<PhongDisplayDTO> GetAllRoomsForDisplay()
        {
            using var ctx = new QuanLyKhachSanContext();
            var today = DateTime.Today;

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
                var activeBookings = room.ChiTietDatPhongs?
                    .Where(ct => ct.DatPhong != null
                              && ct.DatPhong.TrangThaiDat != 3
                              && ct.DatPhong.TrangThaiDat != 4
                              && ct.DatPhong.TrangThaiDat != 5)
                    .ToList();

                var isCheckInToday = room.TrangThai == 1
                    && (activeBookings?.Any(ct =>
                        ct.NgayCheckIn.Date == today &&
                        ct.DatPhong.TrangThaiDat == 1) ?? false);

                var isCheckOutToday = (room.TrangThai == 2 || room.TrangThai == 3)
                    && (activeBookings?.Any(ct => ct.NgayCheckOut.Date == today) ?? false);

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
        {
            using var ctx = new QuanLyKhachSanContext();

            return ctx.ChiTietDatPhongs
                .AsNoTracking()
                .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                .Include(c => c.ChiTietDichVus).ThenInclude(dv => dv.DichVu)
                .Where(c => c.MaPhong == maPhong
                    && (c.DatPhong.TrangThaiDat == 1 || c.DatPhong.TrangThaiDat == 2))
                .OrderByDescending(c => c.MaChiTietDatPhong)
                .FirstOrDefault();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.Services
{
    public class StatusTransitionService : IStatusTransitionService
    {
        private static readonly TimeSpan CheckoutDeadline = TimeSpan.FromHours(12); // 12:00 PM
        public void RunDailyTransitions()
        {
            using var ctx = new QuanLyKhachSanContext();
            var today = DateTime.Today;
            var now = DateTime.Now;
            var dirty = false;

            dirty |= ReconcileOccupiedRoomStatuses(ctx, now);
            dirty |= MarkNoShows(ctx, today, now);

            if (dirty)
                ctx.SaveChanges();
        }

        private static bool ReconcileOccupiedRoomStatuses(QuanLyKhachSanContext ctx, DateTime now)
        {
            var dirty = false;

            var activeChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.DatPhong.TrangThaiDat == 2 &&
                    ct.TrangThaiSegment == TrangThaiSegment.DangO)
                .AsEnumerable()
                .GroupBy(ct => ct.MaPhong)
                .Select(g => g
                    .OrderByDescending(ct => ct.NgayCheckIn)
                    .ThenByDescending(ct => ct.MaChiTietDatPhong)
                    .First())
                .ToList();

            foreach (var ct in activeChiTiets)
            {
                var expectedStatus = now >= ct.NgayCheckOut ? 3 : 2;
                if (ct.Phong.TrangThai != expectedStatus)
                {
                    ct.Phong.TrangThai = expectedStatus;
                    dirty = true;
                }
            }

            return dirty;
        }

        private static bool MarkNoShows(QuanLyKhachSanContext ctx, DateTime today, DateTime now)
        {
            var dirty = false;

            var noShowChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.DatPhong.TrangThaiDat == 1 &&
                    ct.NgayCheckIn.Date < today &&
                    ct.Phong.TrangThai == 1)
                .ToList();

            foreach (var ct in noShowChiTiets)
            {
                var dat = ct.DatPhong;
                var phong = ct.Phong;

                dat.TrangThaiDat = 5;

                if (dat.TienCoc > 0 && dat.TrangThaiCoc == 0)
                {
                    dat.TrangThaiCoc = 2;

                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = dat.MaDatPhong,
                        LoaiGiaoDich = 2,
                        SoTien = dat.TienCoc,
                        ThoiGian = now,
                        MaNhanVien = LoginSession.CurrentNhanVienId,
                        GhiChu = $"Auto no-show: phong {phong.TenPhong}, ngay nhan du kien {ct.NgayCheckIn:dd/MM/yyyy}. Coc {dat.TienCoc:#,0} VND chuyen doanh thu."
                    });
                }

                phong.TrangThai = 0;
                dirty = true;
            }

            return dirty;
        }
    }
}

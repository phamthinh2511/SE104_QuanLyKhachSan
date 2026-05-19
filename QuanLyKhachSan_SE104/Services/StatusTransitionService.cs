using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.Services
{
    public class StatusTransitionService : IStatusTransitionService
    {
        public void RunDailyTransitions()
        {
            using var ctx = new QuanLyKhachSanContext();
            var today = DateTime.Today;
            var now = DateTime.Now;
            var dirty = false;

            dirty |= MarkOverdueRooms(ctx, today);
            dirty |= MarkNoShows(ctx, today, now);

            if (dirty)
                ctx.SaveChanges();
        }

        private static bool MarkOverdueRooms(QuanLyKhachSanContext ctx, DateTime today)
        {
            var dirty = false;

            var overdueChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.NgayCheckOut < today &&
                    (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                    (ct.Phong.TrangThai == 1 || ct.Phong.TrangThai == 2) &&
                    !ctx.ChiTietDatPhongs.Any(next =>
                        next.MaDatPhong == ct.MaDatPhong &&
                        next.MaChiTietDatPhong != ct.MaChiTietDatPhong &&
                        next.NgayCheckIn == ct.NgayCheckOut) &&
                    ct.Phong.TrangThai != 3)
                .ToList();

            foreach (var ct in overdueChiTiets)
            {
                ct.Phong.TrangThai = 3;
                dirty = true;
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

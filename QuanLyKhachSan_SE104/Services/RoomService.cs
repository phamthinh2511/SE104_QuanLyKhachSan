using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public class RoomService
    {
        public List<PhongDTO> TimPhongTrong(RoomSearchDTO req, int? excludeMaPhong = null)
        {
            using var ctx = new QuanLyKhachSanContext();

            var busyIds = ctx.ChiTietDatPhongs
                .Where(ct =>
                    (ct.TrangThaiSegment == TrangThaiSegment.ChoNhanPhong ||
                     ct.TrangThaiSegment == TrangThaiSegment.DangO) &&
                    ct.NgayCheckIn < req.NgayCheckOut &&
                    ct.NgayCheckOut > req.NgayCheckIn)
                .Select(ct => ct.MaPhong)
                .Distinct()
                .ToList();

            var query = ctx.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => !busyIds.Contains(p.MaPhong) && p.TrangThai == 0 && !p.IsDeleted);

            if (excludeMaPhong.HasValue)
                query = query.Where(p => p.MaPhong != excludeMaPhong.Value);

            if (req.SoTang.HasValue && req.SoTang.Value > 0)
                query = query.Where(p => p.SoTang == req.SoTang.Value);

            if (req.MaLoaiPhong.HasValue && req.MaLoaiPhong.Value > 0)
                query = query.Where(p => p.MaLoaiPhong == req.MaLoaiPhong.Value);

            return query
                .OrderBy(p => p.TenPhong)
                .Select(p => new PhongDTO
                {
                    MaPhong = p.MaPhong,
                    TenPhong = p.TenPhong,
                    SoTang = p.SoTang,
                    GiaMacDinh = p.LoaiPhong.GiaMacDinh,
                    TrangThaiDonDep = p.TrangThaiDonDep,
                    TenLoaiPhong = p.LoaiPhong.TenLoaiPhong,
                    SoNguoiToiDa = p.LoaiPhong.SoNguoiToiDa
                })
                .ToList();
        }

        public decimal TinhTienCocToiThieu(IEnumerable<int> maPhongList)
        {
            var ids = maPhongList?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return 0;

            using var ctx = new QuanLyKhachSanContext();

            return ctx.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => ids.Contains(p.MaPhong))
                .Select(p => p.LoaiPhong.GiaMacDinh)
                .AsEnumerable()
                .DefaultIfEmpty(0)
                .Min();
        }

        public int? ToggleCleaningStatus(int maPhong)
        {
            using var ctx = new QuanLyKhachSanContext();
            var phong = ctx.Phongs.Find(maPhong);
            if (phong == null) return null;

            phong.TrangThaiDonDep = phong.TrangThaiDonDep switch
            {
                0 => 1,
                1 => 0,
                2 => 0,
                _ => 0
            };

            ctx.SaveChanges();
            return phong.TrangThaiDonDep;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;
using System.Data;

namespace QuanLyKhachSan_SE104.Services
{
    public class BookingService
    {
        public BookingResult TaoDatPhong(BookingRequestDTO req)
        {
            var validation = ValidateBookingRequest(req);
            if (validation != null) return validation;

            try
            {
                using var ctx = new QuanLyKhachSanContext();
                using var tx = ctx.Database.BeginTransaction(IsolationLevel.RepeatableRead);

                var maPhongList = req.MaPhongList.Distinct().ToList();
                var conflictRoomId = FindConflictingRoomId(ctx, maPhongList, req.NgayCheckIn, req.NgayCheckOut);
                if (conflictRoomId.HasValue)
                {
                    tx.Rollback();
                    return BookingResult.Conflict(conflictRoomId.Value);
                }

                var rooms = ctx.Phongs
                    .Include(p => p.LoaiPhong)
                    .Where(p => maPhongList.Contains(p.MaPhong) && !p.IsDeleted)
                    .ToList();

                if (rooms.Count != maPhongList.Count)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Mot hoac nhieu phong khong ton tai.");
                }

                var customer = new KhachHang
                {
                    HoTen = req.HoTen.Trim(),
                    SDT = req.SDT.Trim(),
                    CCCD_Passport = req.CCCD.Trim(),
                    GioiTinh = req.GioiTinh.Trim(),
                    QuocTich = req.QuocTich.Trim(),
                    DiaChi = string.Empty
                };

                ctx.KhachHangs.Add(customer);
                ctx.SaveChanges();

                var trangThaiDat = req.IsWalkIn ? 2 : 1;
                var depositAmount = req.IsWalkIn ? 0 : req.TienCoc;

                var booking = new DatPhong
                {
                    MaKhachHang = customer.MaKhachHang,
                    NgayDat = DateTime.Now,
                    TrangThaiDat = trangThaiDat,
                    MaNhanVien = req.MaNhanVien,
                    TienCoc = depositAmount,
                    TrangThaiCoc = 0
                };

                ctx.DatPhongs.Add(booking);
                ctx.SaveChanges();

                if (depositAmount > 0)
                {
                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = booking.MaDatPhong,
                        LoaiGiaoDich = 0,
                        SoTien = depositAmount,
                        ThoiGian = DateTime.Now,
                        MaNhanVien = req.MaNhanVien,
                        GhiChu = $"Thu coc khi dat phong {string.Join(", ", rooms.Select(r => r.TenPhong))}"
                    });
                }

                foreach (var room in rooms)
                {
                    room.TrangThai = req.IsWalkIn ? 2 : 1;

                    ctx.ChiTietDatPhongs.Add(new ChiTietDatPhong
                    {
                        MaDatPhong = booking.MaDatPhong,
                        MaPhong = room.MaPhong,
                        NgayCheckIn = req.NgayCheckIn,
                        NgayCheckOut = req.NgayCheckOut,
                        GiaDat = room.LoaiPhong?.GiaMacDinh ?? 0,
                        SoNguoi = 1
                    });
                }

                ctx.SaveChanges();
                tx.Commit();

                var result = BookingResult.Success(booking.MaDatPhong);
                result.Message = req.IsWalkIn
                    ? "Check-in khach le thanh cong."
                    : "Dat phong thanh cong.";
                return result;
            }
            catch (Exception ex)
            {
                return BookingResult.Error("Loi luu don dat phong: " + GetExceptionMessage(ex));
            }
        }

        public BookingResult DoiPhong(DoiPhongRequestDTO req)
        {
            if (req.MaDatPhong <= 0 || req.MaChiTietDatPhong <= 0)
                return BookingResult.ValidationError("Thong tin dat phong khong hop le.");

            if (req.MaPhongMoi <= 0 || req.MaPhongCu <= 0)
                return BookingResult.ValidationError("Thong tin phong khong hop le.");

            if (req.MaPhongMoi == req.MaPhongCu)
                return BookingResult.ValidationError("Vui long chon phong khac voi phong hien tai.");

            try
            {
                using var ctx = new QuanLyKhachSanContext();
                using var tx = ctx.Database.BeginTransaction();
                var thoiDiemDoiPhong = DateTime.Now;

                var dat = ctx.DatPhongs.Find(req.MaDatPhong);
                if (dat == null)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Khong tim thay thong tin dat phong.");
                }

                var ctOld = ctx.ChiTietDatPhongs.Find(req.MaChiTietDatPhong);
                if (ctOld == null)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Khong tim thay chi tiet dat phong cu.");
                }

                if (req.NgayCheckOut <= thoiDiemDoiPhong)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Ngay check-out moi phai sau thoi diem doi phong.");
                }

                var oldPhong = ctx.Phongs.Find(req.MaPhongCu);
                var newPhong = ctx.Phongs
                    .Include(p => p.LoaiPhong)
                    .FirstOrDefault(p => p.MaPhong == req.MaPhongMoi && !p.IsDeleted);

                if (newPhong == null)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Khong tim thay phong moi.");
                }

                var hasConflict = ctx.ChiTietDatPhongs.Any(ct =>
                    ct.MaPhong == req.MaPhongMoi &&
                    ct.MaChiTietDatPhong != req.MaChiTietDatPhong &&
                    (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                    ct.NgayCheckIn < req.NgayCheckOut &&
                    ct.NgayCheckOut > thoiDiemDoiPhong);

                if (hasConflict)
                {
                    tx.Rollback();
                    return BookingResult.Conflict(req.MaPhongMoi);
                }

                ctOld.NgayCheckOut = thoiDiemDoiPhong;

                if (oldPhong != null)
                {
                    oldPhong.TrangThai = 0;
                    oldPhong.TrangThaiDonDep = 1;
                }

                newPhong.TrangThai = dat.TrangThaiDat == 2 ? 2 : 1;

                ctx.ChiTietDatPhongs.Add(new ChiTietDatPhong
                {
                    MaDatPhong = dat.MaDatPhong,
                    MaPhong = req.MaPhongMoi,
                    NgayCheckIn = thoiDiemDoiPhong,
                    NgayCheckOut = req.NgayCheckOut,
                    GiaDat = newPhong.LoaiPhong?.GiaMacDinh ?? 0,
                    SoNguoi = ctOld.SoNguoi
                });

                if (dat.TienCoc > 0)
                {
                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = dat.MaDatPhong,
                        LoaiGiaoDich = 3,
                        SoTien = dat.TienCoc,
                        ThoiGian = thoiDiemDoiPhong,
                        MaNhanVien = req.MaNhanVien,
                        GhiChu = $"Doi phong: {oldPhong?.TenPhong} -> {newPhong.TenPhong}. Coc giu nguyen.",
                        MaDatPhongMoi = null
                    });
                }

                ctx.SaveChanges();
                tx.Commit();

                var result = BookingResult.Success(dat.MaDatPhong);
                result.Message = $"Doi sang phong {newPhong.TenPhong} thanh cong.";
                return result;
            }
            catch (Exception ex)
            {
                return BookingResult.Error("Loi doi phong: " + GetExceptionMessage(ex));
            }
        }

        public BookingResult GiaHan(GiaHanRequestDTO req)
        {
            if (req.MaChiTietDatPhong <= 0)
                return BookingResult.ValidationError("Thong tin dat phong khong hop le.");

            try
            {
                using var ctx = new QuanLyKhachSanContext();
                using var tx = ctx.Database.BeginTransaction();

                var ct = ctx.ChiTietDatPhongs.Find(req.MaChiTietDatPhong);
                if (ct == null)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Khong tim thay thong tin dat phong.");
                }

                if (req.NgayCheckOutMoi <= ct.NgayCheckOut)
                {
                    tx.Rollback();
                    return BookingResult.ValidationError("Ngay check-out moi phai sau ngay check-out hien tai.");
                }

                var isOverbooked = ctx.ChiTietDatPhongs.Any(other =>
                    other.MaPhong == ct.MaPhong &&
                    other.MaChiTietDatPhong != ct.MaChiTietDatPhong &&
                    (other.DatPhong.TrangThaiDat == 1 || other.DatPhong.TrangThaiDat == 2) &&
                    other.NgayCheckIn < req.NgayCheckOutMoi &&
                    other.NgayCheckOut > ct.NgayCheckOut);

                if (isOverbooked)
                {
                    tx.Rollback();
                    return BookingResult.Conflict(ct.MaPhong);
                }

                ct.NgayCheckOut = req.NgayCheckOutMoi;

                var phong = ctx.Phongs.Find(ct.MaPhong);
                if (phong != null) phong.TrangThai = 2;

                var dat = ctx.DatPhongs.Find(ct.MaDatPhong);
                if (dat != null && dat.TrangThaiDat != 2) dat.TrangThaiDat = 2;

                ctx.SaveChanges();
                tx.Commit();

                var result = BookingResult.Success(ct.MaDatPhong);
                result.Message = $"Gia han phong den {req.NgayCheckOutMoi:dd/MM/yyyy HH:mm} thanh cong.";
                return result;
            }
            catch (Exception ex)
            {
                return BookingResult.Error("Loi gia han: " + GetExceptionMessage(ex));
            }
        }

        private static BookingResult? ValidateBookingRequest(BookingRequestDTO req)
        {
            if (req.MaPhongList == null || req.MaPhongList.Count == 0)
                return BookingResult.ValidationError("Vui long chon it nhat mot phong.");

            if (req.NgayCheckOut <= req.NgayCheckIn)
                return BookingResult.ValidationError("Ngay check-out phai sau ngay check-in.");

            if (string.IsNullOrWhiteSpace(req.HoTen))
                return BookingResult.ValidationError("Vui long nhap ho ten khach hang.");

            if (req.MaNhanVien <= 0)
                return BookingResult.ValidationError("Thong tin nhan vien khong hop le.");

            return null;
        }

        private static int? FindConflictingRoomId(
            QuanLyKhachSanContext ctx,
            List<int> maPhongList,
            DateTime ngayCheckIn,
            DateTime ngayCheckOut)
        {
            return ctx.ChiTietDatPhongs
                .Where(ct =>
                    maPhongList.Contains(ct.MaPhong) &&
                    (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                    ct.NgayCheckIn < ngayCheckOut &&
                    ct.NgayCheckOut > ngayCheckIn)
                .Select(ct => (int?)ct.MaPhong)
                .FirstOrDefault();
        }

        private static string GetExceptionMessage(Exception ex)
        {
            return ex.InnerException?.Message ?? ex.Message;
        }
    }
}

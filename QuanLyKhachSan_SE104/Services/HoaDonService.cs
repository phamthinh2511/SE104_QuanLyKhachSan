using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public class HoaDonService : IHoaDonService
    {
        private const string PaidStatus = "Đã thanh toán";
        private const string ExtensionSurchargeServiceName = "Phụ phí gia hạn phòng";
        private const string EarlyCheckInSurchargeServiceName = "Phụ phí check-in sớm";
        private static readonly TimeSpan StandardCheckInTime = TimeSpan.FromHours(14);

        public InvoiceDetailDTO GetInvoiceDetails(int maDatPhong, int maChiTietDatPhong, int maNhanVienCheckout)
        {
            using var ctx = new QuanLyKhachSanContext();

            var datPhong = ctx.DatPhongs
                .Include(d => d.KhachHang)
                .Include(d => d.NhanVien)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .Include(d => d.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.ChiTietDichVus)
                        .ThenInclude(dv => dv.DichVu)
                .FirstOrDefault(d => d.MaDatPhong == maDatPhong);

            if (datPhong == null)
                throw new InvalidOperationException("Không tìm thấy thông tin đặt phòng.");

            var allSegments = datPhong.ChiTietDatPhongs
                .OrderBy(ct => ct.NgayCheckIn)
                .ThenBy(ct => ct.MaChiTietDatPhong)
                .ToList();

            if (!allSegments.Any())
                throw new InvalidOperationException("Không tìm thấy chi tiết đặt phòng.");

            var activeSegment = (maChiTietDatPhong > 0
                ? allSegments.FirstOrDefault(ct => ct.MaChiTietDatPhong == maChiTietDatPhong)
                : null)
                ?? allSegments.OrderByDescending(ct => ct.MaChiTietDatPhong).First();

            var hoaDon = ctx.HoaDons
                .Include(h => h.NhanVien)
                .FirstOrDefault(h => h.MaDatPhong == maDatPhong);

            var isPaid = hoaDon != null && hoaDon.TrangThaiThanhToan == PaidStatus;
            if (!isPaid)
                EnsureEarlyCheckInSurcharges(ctx, allSegments);

            var now = DateTime.Now;
            var ngayCheckOut = isPaid ? hoaDon!.NgayThanhToan : now;
            var ngayCheckOutHopDong = activeSegment.NgayCheckOut;
            var overdueBySegment = CalculateCumulativeOverdue(allSegments, activeSegment, hoaDon, isPaid, now);

            var segments = allSegments.Select(ct =>
            {
                var isActive = ct.MaChiTietDatPhong == activeSegment.MaChiTietDatPhong;
                var segCheckOut = isActive ? ngayCheckOut : ct.NgayCheckOut;
                var soDem = ResolveNightCount(ct, isActive, segCheckOut, allSegments);
                overdueBySegment.TryGetValue(ct.MaChiTietDatPhong, out var overdue);

                return new PhongSegmentDTO
                {
                    TenPhong = ct.Phong?.TenPhong ?? "—",
                    NgayCheckIn = ct.NgayCheckIn,
                    NgayCheckOut = segCheckOut,
                    SoDem = soDem,
                    GiaMoiDem = ct.GiaDat,
                    IsCurrentRoom = isActive,
                    SoGioQuaHan = overdue.Hours,
                    PhuPhiQuaHan = overdue.Surcharge
                };
            }).ToList();

            var extensionSurcharge = allSegments
                .SelectMany(ct => ct.ChiTietDichVus ?? Enumerable.Empty<ChiTietDichVu>())
                .Where(IsExtensionSurcharge)
                .Sum(x => x.ThanhTien);

            var danhSachDichVu = allSegments
                .SelectMany(ct => ct.ChiTietDichVus ?? Enumerable.Empty<ChiTietDichVu>())
                .Where(x => x.DichVu != null && !IsExtensionSurcharge(x))
                .Select(x => new ChiTietDichVuDTO
                {
                    TenDichVu = x.DichVu.TenDichVu,
                    DonGia = x.DonGia,
                    SoLuong = x.SoLuong
                })
                .ToList();

            var phuPhiMoiGio = activeSegment.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;
            var soGioQuaHan = overdueBySegment.Values.Sum(x => x.Hours);
            var currentOverdueSurcharge = overdueBySegment.Values.Sum(x => x.Surcharge);

            var roomNames = segments.Select(s => s.TenPhong).Distinct().ToList();
            var depositAlreadyApplied = isPaid;
            var tongTienPhong = allSegments.Sum(ct =>
            {
                var isActive = ct.MaChiTietDatPhong == activeSegment.MaChiTietDatPhong;
                var segmentCheckOut = isActive ? ngayCheckOut : ct.NgayCheckOut;
                return GetRoomTotal(ct, isActive, segmentCheckOut, allSegments);
            });
            var tongTienDichVu = danhSachDichVu.Sum(x => x.ThanhTien);
            var phuPhi = extensionSurcharge + currentOverdueSurcharge;
            var depositDeduction = depositAlreadyApplied ? 0 : datPhong.TienCoc;
            var tongThanhToan = isPaid
                ? hoaDon!.TongThanhToan
                : tongTienPhong + tongTienDichVu + phuPhi - depositDeduction;

            return new InvoiceDetailDTO
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaChiTietDatPhongActive = activeSegment.MaChiTietDatPhong,
                MaHoaDonText = hoaDon != null ? $"#HD-{hoaDon.MaHoaDon:D6}" : "Chưa lập",
                TenKhachHang = datPhong.KhachHang?.HoTen ?? "—",
                SdtKhachHang = datPhong.KhachHang?.SDT ?? "—",
                TenNhanVien = ResolveInvoiceEmployeeName(ctx, hoaDon, maNhanVienCheckout, datPhong),
                TenPhong = roomNames.Count > 1 ? string.Join(" → ", roomNames) : roomNames.First(),
                NgayCheckIn = allSegments.First().NgayCheckIn,
                NgayCheckOut = ngayCheckOut,
                NgayCheckOutHopDong = ngayCheckOutHopDong,
                DanhSachSegment = segments,
                DanhSachDichVu = danhSachDichVu,
                SoGioQuaHan = soGioQuaHan,
                PhuPhiMoiGio = phuPhiMoiGio,
                TongTienPhong = tongTienPhong,
                TongTienDichVu = tongTienDichVu,
                PhuPhi = phuPhi,
                TienCoc = datPhong.TienCoc,
                DepositAlreadyApplied = depositAlreadyApplied,
                TongThanhToan = tongThanhToan,
                IsPaid = isPaid,
                TrangThaiThanhToanText = isPaid ? "✔ Đã thanh toán" : "Chờ thanh toán",
                PhuongThucThanhToan = hoaDon?.PhuongThucThanhToan ?? -1,
                PhuongThucThanhToanText = hoaDon != null ? ToPaymentLabel(hoaDon.PhuongThucThanhToan) : "",
                NgayThanhToan = hoaDon?.NgayThanhToan,
                NgayThanhToanText = hoaDon != null ? $"Ngày TT: {hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}" : "",
                GhiChu = hoaDon?.GhiChu ?? ""
            };
        }

        public InvoiceDetailDTO ProcessCheckOut(CheckOutRequestDTO request)
        {
            if (request.MaDatPhong <= 0 || request.MaChiTietDatPhongActive <= 0)
                throw new InvalidOperationException("Thông tin đặt phòng không hợp lệ.");

            if (request.MaNhanVien <= 0)
                throw new InvalidOperationException("Thông tin nhân viên không hợp lệ.");

            using var ctx = new QuanLyKhachSanContext();

            if (ctx.HoaDons.Any(h => h.MaDatPhong == request.MaDatPhong && h.TrangThaiThanhToan == PaidStatus))
                throw new InvalidOperationException("Đặt phòng này đã được thanh toán.");

            var details = GetInvoiceDetails(request.MaDatPhong, request.MaChiTietDatPhongActive, request.MaNhanVien);
            var thanhToanAt = DateTime.Now;

            var hoaDon = new HoaDon
            {
                MaDatPhong = request.MaDatPhong,
                MaNhanVien = request.MaNhanVien,
                TongTienPhong = details.TongTienPhong,
                TongTienDichVu = details.TongTienDichVu,
                PhuPhi = details.PhuPhi,
                TienCoc = details.DepositAlreadyApplied ? 0 : details.TienCoc,
                TongThanhToan = details.TongThanhToan,
                NgayThanhToan = thanhToanAt,
                PhuongThucThanhToan = request.PhuongThucThanhToan,
                GhiChu = request.GhiChu,
                TrangThaiThanhToan = PaidStatus
            };
            ctx.HoaDons.Add(hoaDon);

            var datPhong = ctx.DatPhongs.Find(request.MaDatPhong);
            if (datPhong != null)
            {
                datPhong.TrangThaiDat = 3;
                if (datPhong.TrangThaiCoc == 0)
                    datPhong.TrangThaiCoc = 2;
            }

            if (details.TienCoc > 0 && !details.DepositAlreadyApplied)
            {
                ctx.LichSuCocs.Add(new LichSuCoc
                {
                    MaDatPhong = request.MaDatPhong,
                    LoaiGiaoDich = 2,
                    SoTien = details.TienCoc,
                    ThoiGian = thanhToanAt,
                    MaNhanVien = request.MaNhanVien,
                    GhiChu = $"Khấu trừ cọc khi checkout {details.TenPhong}. " +
                             $"Tổng trước cọc: {details.TongTienPhong + details.TongTienDichVu + details.PhuPhi:#,0}₫. " +
                             $"Thực thu: {details.TongThanhToan:#,0}₫."
                });
            }

            var activeChiTiet = ctx.ChiTietDatPhongs.Find(request.MaChiTietDatPhongActive);
            if (activeChiTiet != null)
            {
                activeChiTiet.NgayCheckOut = thanhToanAt;
                activeChiTiet.TrangThaiSegment = TrangThaiSegment.DaCheckOut;

                var activePhong = ctx.Phongs.Find(activeChiTiet.MaPhong);
                if (activePhong != null)
                {
                    activePhong.TrangThai = 0;
                    activePhong.TrangThaiDonDep = 1;
                }
            }

            ctx.SaveChanges();
            return GetInvoiceDetails(request.MaDatPhong, request.MaChiTietDatPhongActive, request.MaNhanVien);
        }

        private static Dictionary<int, OverdueCharge> CalculateCumulativeOverdue(
            List<ChiTietDatPhong> allSegments,
            ChiTietDatPhong activeSegment,
            HoaDon? hoaDon,
            bool isPaid,
            DateTime now)
        {
            var result = new Dictionary<int, OverdueCharge>();

            foreach (var segment in allSegments)
            {
                var hourlyRate = segment.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;
                if (hourlyRate <= 0)
                {
                    result[segment.MaChiTietDatPhong] = new OverdueCharge(0, 0);
                    continue;
                }

                var referenceTime = segment.MaChiTietDatPhong == activeSegment.MaChiTietDatPhong
                    ? (isPaid && hoaDon != null ? hoaDon.NgayThanhToan : now)
                    : ResolveHistoricalSegmentEnd(allSegments, segment);

                var hours = CalculateOverdueHours(referenceTime, segment.NgayCheckOut);

                result[segment.MaChiTietDatPhong] = new OverdueCharge(hours, hours * hourlyRate);
            }

            return result;
        }

        private static DateTime ResolveHistoricalSegmentEnd(List<ChiTietDatPhong> allSegments, ChiTietDatPhong segment)
        {
            var nextSegment = allSegments
                .Where(ct => ct.NgayCheckIn >= segment.NgayCheckIn
                             && ct.MaChiTietDatPhong != segment.MaChiTietDatPhong)
                .OrderBy(ct => ct.NgayCheckIn)
                .ThenBy(ct => ct.MaChiTietDatPhong)
                .FirstOrDefault();

            return nextSegment?.NgayCheckIn ?? segment.NgayCheckOut;
        }

        private static int CalculateOverdueHours(DateTime referenceTime, DateTime deadline)
            => referenceTime > deadline
                ? (int)Math.Floor((referenceTime - deadline).TotalHours)
                : 0;

        private static bool IsExtensionSurcharge(ChiTietDichVu chiTietDichVu)
            => chiTietDichVu.DichVu?.TenDichVu == ExtensionSurchargeServiceName;

        private static void EnsureEarlyCheckInSurcharges(
            QuanLyKhachSanContext ctx,
            List<ChiTietDatPhong> allSegments)
        {
            var firstSegment = allSegments
                .OrderBy(ct => ct.NgayCheckIn)
                .ThenBy(ct => ct.MaChiTietDatPhong)
                .FirstOrDefault();

            if (firstSegment == null)
                return;

            DichVu? surchargeService = null;
            var hasChanges = false;

            foreach (var segment in allSegments)
            {
                var existingEntries = (segment.ChiTietDichVus ?? Enumerable.Empty<ChiTietDichVu>())
                    .Where(x => x.DichVu?.TenDichVu == EarlyCheckInSurchargeServiceName)
                    .ToList();

                if (segment.MaChiTietDatPhong != firstSegment.MaChiTietDatPhong)
                {
                    foreach (var entry in existingEntries)
                    {
                        ctx.ChiTietDichVus.Remove(entry);
                        segment.ChiTietDichVus?.Remove(entry);
                        hasChanges = true;
                    }

                    continue;
                }

                var hourlyRate = segment.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;
                var earlyHours = CalculateEarlyCheckInHours(segment.NgayCheckIn);
                if (hourlyRate <= 0 || earlyHours <= 0)
                {
                    foreach (var entry in existingEntries)
                    {
                        ctx.ChiTietDichVus.Remove(entry);
                        segment.ChiTietDichVus?.Remove(entry);
                        hasChanges = true;
                    }

                    continue;
                }

                var existingEntry = existingEntries.FirstOrDefault();

                if (existingEntry != null)
                {
                    foreach (var duplicateEntry in existingEntries.Skip(1))
                    {
                        ctx.ChiTietDichVus.Remove(duplicateEntry);
                        segment.ChiTietDichVus?.Remove(duplicateEntry);
                        hasChanges = true;
                    }

                    if (existingEntry.SoLuong != earlyHours ||
                        existingEntry.DonGia != hourlyRate ||
                        existingEntry.ThoiGianSuDung != segment.NgayCheckIn)
                    {
                        existingEntry.SoLuong = earlyHours;
                        existingEntry.DonGia = hourlyRate;
                        existingEntry.ThoiGianSuDung = segment.NgayCheckIn;
                        hasChanges = true;
                    }

                    continue;
                }

                surchargeService ??= GetOrCreateEarlyCheckInSurchargeService(ctx);

                var surchargeEntry = new ChiTietDichVu
                {
                    MaChiTietDatPhong = segment.MaChiTietDatPhong,
                    MaDichVu = surchargeService.MaDichVu,
                    SoLuong = earlyHours,
                    DonGia = hourlyRate,
                    ThoiGianSuDung = segment.NgayCheckIn,
                    DichVu = surchargeService,
                    ChiTietDatPhong = segment
                };

                ctx.ChiTietDichVus.Add(surchargeEntry);
                segment.ChiTietDichVus ??= new List<ChiTietDichVu>();
                segment.ChiTietDichVus.Add(surchargeEntry);
                hasChanges = true;
            }

            if (hasChanges)
                ctx.SaveChanges();
        }

        private static int CalculateEarlyCheckInHours(DateTime checkIn)
        {
            var standardCheckIn = checkIn.Date + StandardCheckInTime;
            return checkIn < standardCheckIn
                ? (int)Math.Ceiling((standardCheckIn - checkIn).TotalHours)
                : 0;
        }

        private static DichVu GetOrCreateEarlyCheckInSurchargeService(QuanLyKhachSanContext ctx)
        {
            var service = ctx.DichVus
                .FirstOrDefault(d => d.TenDichVu == EarlyCheckInSurchargeServiceName && !d.IsDeleted);

            if (service != null)
                return service;

            service = new DichVu
            {
                TenDichVu = EarlyCheckInSurchargeServiceName,
                LoaiDichVu = 4,
                DonGia = 0,
                MoTa = "Dịch vụ hệ thống dùng để ghi nhận phụ phí phát sinh khi khách check-in trước 14:00."
            };

            ctx.DichVus.Add(service);
            ctx.SaveChanges();
            return service;
        }

        private static int NormalizeStoredNightCount(ChiTietDatPhong segment)
        {
            if (segment.ThanhTien > 0 && segment.GiaDat > 0)
                return (int)Math.Round(segment.ThanhTien / segment.GiaDat, MidpointRounding.AwayFromZero);

            return Math.Max(0, segment.SoDem);
        }

        private static int ResolveNightCount(
            ChiTietDatPhong segment,
            bool isActive,
            DateTime actualCheckOut,
            List<ChiTietDatPhong> allSegments)
        {
            if (!isActive || IsExtensionOnlySegment(segment, allSegments))
                return NormalizeStoredNightCount(segment);

            return CalculateActualNightCount(segment.NgayCheckIn, actualCheckOut);
        }

        private static decimal GetRoomTotal(
            ChiTietDatPhong segment,
            bool isActive,
            DateTime actualCheckOut,
            List<ChiTietDatPhong> allSegments)
        {
            if (!isActive || IsExtensionOnlySegment(segment, allSegments))
                return segment.ThanhTien > 0 ? segment.ThanhTien : Math.Max(0, segment.SoDem) * segment.GiaDat;

            return ResolveNightCount(segment, isActive, actualCheckOut, allSegments) * segment.GiaDat;
        }

        private static int CalculateActualNightCount(DateTime checkIn, DateTime actualCheckOut)
        {
            if (actualCheckOut <= checkIn)
                return 0;

            var nights = (int)Math.Ceiling((actualCheckOut.Date - checkIn.Date).TotalDays);
            return Math.Max(1, nights);
        }

        private static bool IsExtensionOnlySegment(ChiTietDatPhong segment, List<ChiTietDatPhong> allSegments)
            => segment.SoDem == 0
               && segment.ThanhTien <= 0
               && allSegments.Any(ct =>
                   ct.MaChiTietDatPhong != segment.MaChiTietDatPhong &&
                   ct.MaPhong == segment.MaPhong &&
                   ct.MaDatPhong == segment.MaDatPhong &&
                   ct.NgayCheckOut <= segment.NgayCheckIn);

        private static string ResolveInvoiceEmployeeName(
            QuanLyKhachSanContext ctx,
            HoaDon? hoaDon,
            int maNhanVienCheckout,
            DatPhong datPhong)
        {
            if (hoaDon?.NhanVien?.HoTen != null)
                return hoaDon.NhanVien.HoTen;

            var checkoutEmployee = maNhanVienCheckout > 0
                ? ctx.NhanViens.Find(maNhanVienCheckout)
                : null;

            return checkoutEmployee?.HoTen ?? datPhong.NhanVien?.HoTen ?? "—";
        }

        private static string ToPaymentLabel(int method) => method switch
        {
            0 => "Tiền mặt",
            1 => "Thẻ tín dụng",
            2 => "Chuyển khoản",
            _ => "—"
        };
        private readonly record struct OverdueCharge(int Hours, decimal Surcharge);
    }
}

using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.Services
{
    public class HoaDonService : IHoaDonService
    {
        private const string PaidStatus = "Đã thanh toán";
        private const string ExtensionSurchargeServiceName = "Phụ phí gia hạn phòng";

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
            var now = DateTime.Now;
            var ngayCheckOut = isPaid ? hoaDon!.NgayThanhToan : now;
            var ngayCheckOutHopDong = activeSegment.NgayCheckOut;

            var segments = allSegments.Select(ct =>
            {
                var isActive = ct.MaChiTietDatPhong == activeSegment.MaChiTietDatPhong;
                var segCheckOut = isActive ? ngayCheckOut : ct.NgayCheckOut;
                var storedSoDem = NormalizeStoredNightCount(ct);

                return new PhongSegmentDTO
                {
                    TenPhong = ct.Phong?.TenPhong ?? "—",
                    NgayCheckIn = ct.NgayCheckIn,
                    NgayCheckOut = segCheckOut,
                    SoDem = storedSoDem,
                    GiaMoiDem = ct.GiaDat,
                    IsCurrentRoom = isActive
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
            var soGioQuaHan = CalculateCurrentOverdueHours(isPaid, hoaDon, now, ngayCheckOutHopDong, phuPhiMoiGio);
            var currentOverdueSurcharge = soGioQuaHan * phuPhiMoiGio;

            var roomNames = segments.Select(s => s.TenPhong).Distinct().ToList();
            var depositAlreadyApplied = IsDepositAlreadyApplied(datPhong);
            var tongTienPhong = allSegments.Sum(GetStoredRoomTotal);
            var tongTienDichVu = danhSachDichVu.Sum(x => x.ThanhTien);
            var phuPhi = extensionSurcharge + currentOverdueSurcharge;
            var depositDeduction = depositAlreadyApplied ? 0 : datPhong.TienCoc;
            var tongThanhToan = tongTienPhong + tongTienDichVu + phuPhi - depositDeduction;

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

        private static int CalculateCurrentOverdueHours(
            bool isPaid,
            HoaDon? hoaDon,
            DateTime now,
            DateTime ngayCheckOutHopDong,
            decimal phuPhiMoiGio)
        {
            if (phuPhiMoiGio <= 0)
                return 0;

            var referenceTime = isPaid && hoaDon != null ? hoaDon.NgayThanhToan : now;
            return referenceTime > ngayCheckOutHopDong
                ? (int)Math.Floor((referenceTime - ngayCheckOutHopDong).TotalHours)
                : 0;
        }

        private static bool IsExtensionSurcharge(ChiTietDichVu chiTietDichVu)
            => chiTietDichVu.DichVu?.TenDichVu == ExtensionSurchargeServiceName;

        private static bool IsDepositAlreadyApplied(DatPhong datPhong)
            => datPhong.TrangThaiCoc is 1 or 2 or 3;

        private static int NormalizeStoredNightCount(ChiTietDatPhong segment)
        {
            if (segment.ThanhTien > 0 && segment.GiaDat > 0)
                return (int)Math.Round(segment.ThanhTien / segment.GiaDat, MidpointRounding.AwayFromZero);

            return Math.Max(0, segment.SoDem);
        }

        private static decimal GetStoredRoomTotal(ChiTietDatPhong segment)
            => segment.ThanhTien > 0 ? segment.ThanhTien : Math.Max(0, segment.SoDem) * segment.GiaDat;

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
    }
}

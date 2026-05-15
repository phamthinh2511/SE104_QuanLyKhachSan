using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.HoaDon;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.ViewModel.HoaDonVM
{
    public class HoaDonViewModel : INotifyPropertyChanged
    {
        // ── TODO: replace with LoginSession.CurrentUserId ─────────────────────
        private const int STAFF_ID = 1;

        // ══════════════════════════════════════════════
        //  Internal state
        // ══════════════════════════════════════════════
        private readonly int _maDatPhong;
        private readonly int _maNhanVien;
        private readonly int _maChiTietDatPhong;

        private HoaDon _hoaDon;

        public Action CloseAction { get; set; }
        // ══════════════════════════════════════════════
        //  Booking info (read-only display)
        // ══════════════════════════════════════════════
        public int MaDatPhong { get; private set; }
        public string MaHoaDonText => _hoaDon != null ? $"#HD-{_hoaDon.MaHoaDon:D6}" : "Chưa lập";
        public string TenKhachHang { get; private set; }
        public string SdtKhachHang { get; private set; }
        public string TenPhong { get; private set; }
        public string TenNhanVien { get; private set; }
        public DateTime NgayCheckIn { get; private set; }
        public DateTime NgayCheckOut { get; private set; }
        public DateTime NgayCheckOutHopDong { get; private set; }
        public decimal GiaDatMoiDem { get; private set; }

        // ══════════════════════════════════════════════
        //  Overdue
        // ══════════════════════════════════════════════
        public int SoGioQuaHan { get; private set; }
        public decimal PhuPhiMoiGio { get; private set; }

        public string SoGioQuaHanText =>
            SoGioQuaHan > 0
                ? $"⚠️  Quá hạn {SoGioQuaHan} giờ  ×  {PhuPhiMoiGio:#,0}₫/giờ  =  {SoGioQuaHan * PhuPhiMoiGio:#,0}₫"
                : "";
        public bool HasOverdue => SoGioQuaHan > 0;

        // ══════════════════════════════════════════════
        //  Số đêm
        // ══════════════════════════════════════════════
        public int SoDemHopDong =>
            Math.Max(1, (int)Math.Ceiling((NgayCheckOutHopDong - NgayCheckIn).TotalDays));

        public int SoDemGiaHan =>
            NgayCheckOut.Date > NgayCheckOutHopDong.Date
                ? (int)Math.Ceiling((NgayCheckOut - NgayCheckOutHopDong).TotalDays)
                : 0;

        private int _soDem;
        public int SoDem
        {
            get => _soDem;
            set { _soDem = value; OnPropertyChanged(); }
        }

        public string SoDemText => $"({SoDem} đêm × {GiaDatMoiDem:#,0}₫)";

        // ══════════════════════════════════════════════
        //  Editable surcharge (PhuPhi)
        // ══════════════════════════════════════════════
        private string _phuPhiInput = "0";
        public string PhuPhiInput
        {
            get => _phuPhiInput;
            set
            {
                _phuPhiInput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PhuPhiText));
                OnPropertyChanged(nameof(TongThanhToanText));
                OnPropertyChanged(nameof(TongThanhToan));
            }
        }

        private decimal ParsedPhuPhi
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_phuPhiInput)) return 0;
                string clean = _phuPhiInput.Replace(",", "").Replace(".", "").Trim();
                return decimal.TryParse(clean, out var v) ? v : 0;
            }
        }

        // ══════════════════════════════════════════════
        //  Deposit — read from DatPhong.TienCoc in DB
        // ══════════════════════════════════════════════

        public decimal TienCoc { get; private set; }

        private bool _depositAlreadyApplied = false;

        // ══════════════════════════════════════════════
        //  Charge totals
        // ══════════════════════════════════════════════
        public decimal TongTienPhong => GiaDatMoiDem * SoDem;
        public decimal TongTienDichVu => DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0;

        public decimal TongThanhToan =>
            TongTienPhong + TongTienDichVu + ParsedPhuPhi
            - (_depositAlreadyApplied ? 0 : TienCoc);

        public string TongTienPhongText => $"{TongTienPhong:#,0}₫";
        public string TongTienDichVuText => $"{TongTienDichVu:#,0}₫";
        public string PhuPhiText => $"{ParsedPhuPhi:#,0}₫";
        public string TienCocText =>
            _depositAlreadyApplied
                ? $"- {TienCoc:#,0}₫  (đã trừ trước)"
                : $"- {TienCoc:#,0}₫";
        public string TongThanhToanText => $"{TongThanhToan:#,0}₫";

        // ══════════════════════════════════════════════
        //  Payment state
        // ══════════════════════════════════════════════
        private bool _isPaid = false;
        public bool IsPaid
        {
            get => _isPaid;
            private set
            {
                _isPaid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotPaid));
                OnPropertyChanged(nameof(TrangThaiThanhToan));
            }
        }
        public bool IsNotPaid => !_isPaid;
        public string TrangThaiThanhToan => _isPaid ? "✔ Đã thanh toán" : "Chờ thanh toán";

        private int _phuongThucThanhToan = -1;
        private string _ngayThanhToanText = "";
        public string PhuongThucThanhToanText { get; private set; } = "";
        public string NgayThanhToanText => _ngayThanhToanText;

        // ══════════════════════════════════════════════
        //  Services
        // ══════════════════════════════════════════════
        private ObservableCollection<ChiTietDichVuDTO> _danhSachDichVu = new();
        public ObservableCollection<ChiTietDichVuDTO> DanhSachDichVu
        {
            get => _danhSachDichVu;
            private set
            {
                _danhSachDichVu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TongTienDichVuText));
                OnPropertyChanged(nameof(TongThanhToanText));
                OnPropertyChanged(nameof(IsNoService));
            }
        }
        public bool IsNoService => DanhSachDichVu == null || DanhSachDichVu.Count == 0;

        // ══════════════════════════════════════════════
        //  GhiChu
        // ══════════════════════════════════════════════
        private string _ghiChu = "";
        public string GhiChu
        {
            get => _ghiChu;
            set { _ghiChu = value; OnPropertyChanged(); }
        }

        // ══════════════════════════════════════════════
        //  Commands
        // ══════════════════════════════════════════════
        public ICommand CheckOutCommand { get; }
        public ICommand InHoaDonCommand { get; }

        // ══════════════════════════════════════════════
        //  Constructor
        // ══════════════════════════════════════════════
        public HoaDonViewModel(int maDatPhong, int maChiTietDatPhong, int maNhanVien)
        {
            _maDatPhong = maDatPhong;
            _maChiTietDatPhong = maChiTietDatPhong;
            _maNhanVien = maNhanVien;

            CheckOutCommand = new RelayCommand(ExecuteCheckOut, () => IsNotPaid);
            InHoaDonCommand = new RelayCommand(ExecuteInHoaDon);

            LoadData();
        }

        // ══════════════════════════════════════════════
        //  LoadData
        // ══════════════════════════════════════════════
        private void LoadData()
        {
            try
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
                    .FirstOrDefault(d => d.MaDatPhong == _maDatPhong);

                if (datPhong == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin đặt phòng.", "Lỗi");
                    return;
                }

                MaDatPhong = datPhong.MaDatPhong;
                TenKhachHang = datPhong.KhachHang?.HoTen ?? "—";
                SdtKhachHang = datPhong.KhachHang?.SDT ?? "—";
                TenNhanVien = datPhong.NhanVien?.HoTen ?? "—";

                TienCoc = datPhong.TienCoc;

                _depositAlreadyApplied = datPhong.TrangThaiCoc == 2;

                // ── ChiTiet for this specific room ────────────────────────────
                var chiTiet = _maChiTietDatPhong > 0
                    ? datPhong.ChiTietDatPhongs
                        .FirstOrDefault(ct => ct.MaChiTietDatPhong == _maChiTietDatPhong)
                    : datPhong.ChiTietDatPhongs.FirstOrDefault();

                if (chiTiet != null)
                {
                    TenPhong = chiTiet.Phong?.TenPhong ?? "—";
                    NgayCheckIn = chiTiet.NgayCheckIn;
                    NgayCheckOut = DateTime.Now;                  // actual checkout moment
                    NgayCheckOutHopDong = chiTiet.NgayCheckOut;          // contract deadline
                    GiaDatMoiDem = chiTiet.GiaDat;
                    PhuPhiMoiGio = chiTiet.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;

                    int soDemThucTe = Math.Max(1, (int)Math.Ceiling((NgayCheckOut - NgayCheckIn).TotalDays));

                    if (NgayCheckOut <= NgayCheckOutHopDong)
                    {
                        // On-time or early checkout
                        SoDem = soDemThucTe;
                        SoGioQuaHan = 0;
                        _phuPhiInput = "0";
                    }
                    else
                    {
                        // Late checkout — overdue surcharge applies
                        // (If the guest had already extended via GiaHan, NgayCheckOutHopDong
                        //  would have been updated in DB, so this branch only fires for
                        //  genuine overdue situations.)
                        SoDem = Math.Max(1, (int)Math.Ceiling(
                            (NgayCheckOutHopDong - NgayCheckIn).TotalDays));

                        double lateHours = (NgayCheckOut - NgayCheckOutHopDong).TotalHours;
                        SoGioQuaHan = (int)Math.Floor(lateHours);
                        _phuPhiInput = (SoGioQuaHan * PhuPhiMoiGio).ToString("N0");
                    }

                    // Services
                    var dv = chiTiet.ChiTietDichVus?
                        .Where(x => x.DichVu != null)
                        .Select(x => new ChiTietDichVuDTO
                        {
                            TenDichVu = x.DichVu.TenDichVu,
                            DonGia = x.DonGia,
                            SoLuong = x.SoLuong
                        }) ?? Enumerable.Empty<ChiTietDichVuDTO>();

                    DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>(dv);
                }

                // ── Check if already paid ─────────────────────────────────────
                _hoaDon = ctx.HoaDons.FirstOrDefault(h => h.MaDatPhong == _maDatPhong);
                if (_hoaDon != null && _hoaDon.TrangThaiThanhToan == "Đã thanh toán")
                {
                    _isPaid = true;
                    _phuPhiInput = _hoaDon.PhuPhi.ToString();
                    GhiChu = _hoaDon.GhiChu ?? "";
                    _phuongThucThanhToan = _hoaDon.PhuongThucThanhToan;
                    PhuongThucThanhToanText = ToPaymentLabel(_hoaDon.PhuongThucThanhToan);
                    _ngayThanhToanText = $"Ngày TT: {_hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";

                    // Ensure rooms are freed (idempotent — safe to run on re-view)
                    var maPhongs = datPhong.ChiTietDatPhongs.Select(ct => ct.MaPhong).ToList();
                    var phongs = ctx.Phongs
                        .Where(p => maPhongs.Contains(p.MaPhong) && p.TrangThai != 0)
                        .ToList();
                    foreach (var p in phongs) { p.TrangThai = 0; p.TrangThaiDonDep = 1; }
                    if (phongs.Any()) ctx.SaveChanges();
                }
                else
                {
                    _isPaid = false;
                    _hoaDon = null;
                }

                NotifyAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu hóa đơn: " + ex.Message, "Lỗi");
            }
        }

        // ══════════════════════════════════════════════
        //  ExecuteCheckOut
        //  Total = Room + Services + Surcharge - Deposit
        //  Writes LichSuCoc audit row, sets TrangThaiCoc = 2
        // ══════════════════════════════════════════════
        private void ExecuteCheckOut()
        {
            if (IsPaid) return;

            var dialog = new PaymentMethodDialog();
            if (dialog.ShowDialog() != true) return;

            int phuongThuc = dialog.SelectedMethod;

            decimal tongCuoi = TongThanhToan;

            string depositLine = TienCoc > 0
                ? $"\n  Tiền cọc khấu trừ:  - {TienCoc:#,0}₫"
                : "";

            var confirm = MessageBox.Show(
                $"Phương thức: {ToPaymentLabel(phuongThuc)}\n" +
                "Xác nhận hoàn tất thanh toán?",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new QuanLyKhachSanContext();

                // ── Write invoice ─────────────────────────────────────────────
                var hoaDon = new HoaDon
                {
                    MaDatPhong = _maDatPhong,
                    MaNhanVien = _maNhanVien,
                    TongTienPhong = TongTienPhong,
                    TongTienDichVu = TongTienDichVu,
                    PhuPhi = ParsedPhuPhi,
                    TienCoc = TienCoc,          // recorded for reference on the invoice
                    TongThanhToan = tongCuoi,
                    NgayThanhToan = DateTime.Now,
                    PhuongThucThanhToan = phuongThuc,
                    GhiChu = GhiChu,
                    TrangThaiThanhToan = "Đã thanh toán"
                };
                ctx.HoaDons.Add(hoaDon);

                // ── Update booking status → Đã trả phòng (3) ─────────────────
                var datPhong = ctx.DatPhongs.Find(_maDatPhong);
                if (datPhong != null)
                {
                    datPhong.TrangThaiDat = 3;   // Đã trả phòng

                    // Feature 5: Mark deposit as applied (Rule: applied at checkout)
                    // Only update if it hasn't been forfeited already (no-show sets it to 2 earlier)
                    if (datPhong.TrangThaiCoc == 0)
                        datPhong.TrangThaiCoc = 2;   // Đã thu vào doanh thu
                }

                // ── Feature 5: Write deposit audit log ────────────────────────
                if (TienCoc > 0 && !_depositAlreadyApplied)
                {
                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = _maDatPhong,
                        LoaiGiaoDich = 2,               // Thu doanh thu (applied at checkout)
                        SoTien = TienCoc,
                        ThoiGian = DateTime.Now,
                        MaNhanVien = _maNhanVien,
                        GhiChu = $"Khấu trừ cọc khi checkout phòng {TenPhong}. " +
                                       $"Tổng hóa đơn trước cọc: {TongTienPhong + TongTienDichVu + ParsedPhuPhi:#,0}₫. " +
                                       $"Thực thu: {tongCuoi:#,0}₫."
                    });
                }

                // ── Free rooms + set cleaning status ─────────────────────────
                var maPhongs = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .Select(ct => ct.MaPhong)
                    .ToList();

                var phongList = ctx.Phongs.Where(p => maPhongs.Contains(p.MaPhong)).ToList();
                foreach (var p in phongList)
                {
                    p.TrangThai = 0;   // Trống
                    p.TrangThaiDonDep = 1;  // Cần dọn
                }

                // Stamp actual checkout time on all ChiTietDatPhong rows
                var chiTietRows = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .ToList();
                foreach (var ct in chiTietRows)
                    ct.NgayCheckOut = NgayCheckOut;

                ctx.SaveChanges();

                // ── Update local state ────────────────────────────────────────
                _hoaDon = hoaDon;
                _depositAlreadyApplied = true;
                _phuongThucThanhToan = phuongThuc;
                PhuongThucThanhToanText = ToPaymentLabel(phuongThuc);
                _ngayThanhToanText = $"Ngày TT: {hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";
                IsPaid = true;

                NotifyAll();
                MessageBox.Show("Thanh toán thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu hóa đơn: " + (ex.InnerException?.Message ?? ex.Message), "Lỗi");
            }
        }

        private void ExecuteInHoaDon()
        {
            MessageBox.Show("Tính năng in hóa đơn đang phát triển.", "Thông báo");
        }

        // ══════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════
        private static string ToPaymentLabel(int method) => method switch
        {
            0 => "Tiền mặt",
            1 => "Thẻ tín dụng",
            2 => "Chuyển khoản",
            _ => "—"
        };

        private void NotifyAll()
        {
            OnPropertyChanged(nameof(MaDatPhong));
            OnPropertyChanged(nameof(MaHoaDonText));
            OnPropertyChanged(nameof(TenKhachHang));
            OnPropertyChanged(nameof(SdtKhachHang));
            OnPropertyChanged(nameof(TenPhong));
            OnPropertyChanged(nameof(TenNhanVien));
            OnPropertyChanged(nameof(NgayCheckIn));
            OnPropertyChanged(nameof(NgayCheckOut));
            OnPropertyChanged(nameof(NgayCheckOutHopDong));
            OnPropertyChanged(nameof(SoDem));
            OnPropertyChanged(nameof(SoDemText));
            OnPropertyChanged(nameof(SoDemHopDong));
            OnPropertyChanged(nameof(SoDemGiaHan));
            OnPropertyChanged(nameof(GiaDatMoiDem));
            OnPropertyChanged(nameof(SoGioQuaHan));
            OnPropertyChanged(nameof(SoGioQuaHanText));
            OnPropertyChanged(nameof(HasOverdue));
            OnPropertyChanged(nameof(PhuPhiMoiGio));
            OnPropertyChanged(nameof(TienCoc));
            OnPropertyChanged(nameof(TienCocText));
            OnPropertyChanged(nameof(TongTienPhong));
            OnPropertyChanged(nameof(TongTienPhongText));
            OnPropertyChanged(nameof(TongTienDichVuText));
            OnPropertyChanged(nameof(PhuPhiText));
            OnPropertyChanged(nameof(TongThanhToan));
            OnPropertyChanged(nameof(TongThanhToanText));
            OnPropertyChanged(nameof(TrangThaiThanhToan));
            OnPropertyChanged(nameof(IsPaid));
            OnPropertyChanged(nameof(IsNotPaid));
            OnPropertyChanged(nameof(PhuongThucThanhToanText));
            OnPropertyChanged(nameof(NgayThanhToanText));
            OnPropertyChanged(nameof(MaHoaDonText));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
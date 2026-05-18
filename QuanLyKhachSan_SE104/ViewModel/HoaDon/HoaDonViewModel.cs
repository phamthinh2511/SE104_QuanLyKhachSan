using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.HoaDon;
using System;
using System.Collections.Generic;
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
        // ══════════════════════════════════════════════
        //  Internal state
        // ══════════════════════════════════════════════
        private readonly int _maDatPhong;
        private readonly int _maNhanVien;
        private readonly int _maChiTietDatPhong;

        private HoaDon _hoaDon;
        private int _maChiTietDatPhongActive;      // resolved MaChiTietDatPhong of the open segment

        public Action CloseAction { get; set; }

        // ══════════════════════════════════════════════
        //  Booking header info (read-only display)
        // ══════════════════════════════════════════════
        public int MaDatPhong { get; private set; }
        public string MaHoaDonText => _hoaDon != null ? $"#HD-{_hoaDon.MaHoaDon:D6}" : "Chưa lập";
        public string TenKhachHang { get; private set; }
        public string SdtKhachHang { get; private set; }
        public string TenNhanVien { get; private set; }

        // Derived from ALL segments for the header display
        public string TenPhong { get; private set; }          // "Phòng 203 → Phòng 302" (or single name)
        public DateTime NgayCheckIn { get; private set; }     // first segment's check-in
        public DateTime NgayCheckOut { get; private set; }    // actual checkout moment (DateTime.Now at load time)
        public DateTime NgayCheckOutHopDong { get; private set; } // last segment's contracted checkout

        // ══════════════════════════════════════════════
        //  Room segments (one row per ChiTietDatPhong)
        // ══════════════════════════════════════════════
        private ObservableCollection<PhongSegmentDTO> _danhSachSegment = new();
        public ObservableCollection<PhongSegmentDTO> DanhSachSegment
        {
            get => _danhSachSegment;
            private set { _danhSachSegment = value; OnPropertyChanged(); }
        }

        // ══════════════════════════════════════════════
        //  Overdue — evaluated on the LAST (current) segment only
        // ══════════════════════════════════════════════
        public int SoGioQuaHan { get; private set; }
        public decimal PhuPhiMoiGio { get; private set; }

        public string SoGioQuaHanText =>
            SoGioQuaHan > 0
                ? $"⚠️  Quá hạn {SoGioQuaHan} giờ  ×  {PhuPhiMoiGio:#,0}₫/giờ  =  {SoGioQuaHan * PhuPhiMoiGio:#,0}₫"
                : "";
        public bool HasOverdue => SoGioQuaHan > 0;

        // ══════════════════════════════════════════════
        //  Summary text kept for legacy XAML bindings
        // ══════════════════════════════════════════════
        public string SoDemText
        {
            get
            {
                int totalNights = _danhSachSegment?.Sum(s => s.SoDem) ?? 0;
                int segCount = _danhSachSegment?.Count ?? 0;
                return segCount > 1
                    ? $"({totalNights} đêm tổng cho {segCount} phòng)"
                    : $"({totalNights} đêm × {_danhSachSegment?.FirstOrDefault()?.GiaMoiDem:#,0}₫)";
            }
        }

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
        //  Deposit
        // ══════════════════════════════════════════════
        public decimal TienCoc { get; private set; }
        private bool _depositAlreadyApplied = false;

        // ══════════════════════════════════════════════
        //  Charge totals
        // ══════════════════════════════════════════════
        public decimal TongTienPhong => _danhSachSegment?.Sum(s => s.ThanhTien) ?? 0;
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
        //  Services — aggregated across ALL segments
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
        public HoaDonViewModel(int maDatPhong, int maChiTietDatPhong)
        {
            _maDatPhong = maDatPhong;
            _maChiTietDatPhong = maChiTietDatPhong;
            _maNhanVien = LoginSession.CurrentNhanVienId;

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

                DateTime now = DateTime.Now;

                // ── Phân giải phân đoạn hoạt động ───────────────────────────
                var allSegments = datPhong.ChiTietDatPhongs
                    .OrderBy(ct => ct.NgayCheckIn)
                    .ThenBy(ct => ct.MaChiTietDatPhong)
                    .ToList();

                if (!allSegments.Any())
                {
                    MessageBox.Show("Không tìm thấy chi tiết đặt phòng.", "Lỗi");
                    return;
                }

                var activeSegment = (_maChiTietDatPhong > 0
                    ? allSegments.FirstOrDefault(ct => ct.MaChiTietDatPhong == _maChiTietDatPhong)
                    : null)
                    ?? allSegments.OrderByDescending(ct => ct.MaChiTietDatPhong).First();

                _maChiTietDatPhongActive = activeSegment.MaChiTietDatPhong;

                // Kiểm tra trạng thái hóa đơn trước
                _hoaDon = ctx.HoaDons.FirstOrDefault(h => h.MaDatPhong == _maDatPhong);
                _isPaid = _hoaDon != null && _hoaDon.TrangThaiThanhToan == "Đã thanh toán";

                NgayCheckIn = allSegments.First().NgayCheckIn;
                NgayCheckOut = _isPaid ? _hoaDon.NgayThanhToan : now;
                NgayCheckOutHopDong = activeSegment.NgayCheckOut;

                // ── Vòng lặp phân đoạn (Đã sửa lỗi khai báo & gộp vòng lặp) ──
                var segments = new List<PhongSegmentDTO>();
                foreach (var ct in allSegments)
                {
                    bool isActive = ct.MaChiTietDatPhong == _maChiTietDatPhongActive;
                    DateTime segCheckOut = isActive ? NgayCheckOut : ct.NgayCheckOut;
                    DateTime segCheckIn = ct.NgayCheckIn;

                    // Sử dụng Ceiling để tính tròn đêm lẻ, tối thiểu là 1 đêm
                    int soDem = (int)Math.Max(1, Math.Ceiling((segCheckOut.Date - segCheckIn.Date).TotalDays));

                    segments.Add(new PhongSegmentDTO
                    {
                        TenPhong = ct.Phong?.TenPhong ?? "—",
                        NgayCheckIn = segCheckIn,
                        NgayCheckOut = segCheckOut,
                        SoDem = soDem,
                        GiaMoiDem = ct.GiaDat,
                        //ThanhTien = soDem * ct.GiaDat, // Cần gán giá trị này để tránh Tổng tiền phòng bằng 0
                        IsCurrentRoom = isActive
                    });
                }
                DanhSachSegment = new ObservableCollection<PhongSegmentDTO>(segments);

                // ── Overdue check & Phụ phí ───────────────────────────────────
                PhuPhiMoiGio = activeSegment.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;

                if (_isPaid)
                {
                    _phuPhiInput = _hoaDon.PhuPhi.ToString("N0");
                    GhiChu = _hoaDon.GhiChu ?? "";
                    _phuongThucThanhToan = _hoaDon.PhuongThucThanhToan;
                    PhuongThucThanhToanText = ToPaymentLabel(_hoaDon.PhuongThucThanhToan);
                    _ngayThanhToanText = $"Ngày TT: {_hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";

                    if (_hoaDon.NgayThanhToan > NgayCheckOutHopDong && PhuPhiMoiGio > 0)
                    {
                        SoGioQuaHan = (int)Math.Floor((_hoaDon.NgayThanhToan - NgayCheckOutHopDong).TotalHours);
                    }
                    else SoGioQuaHan = 0;
                }
                else
                {
                    if (now > NgayCheckOutHopDong)
                    {
                        double lateHours = (now - NgayCheckOutHopDong).TotalHours;
                        SoGioQuaHan = (int)Math.Floor(lateHours);
                        _phuPhiInput = (SoGioQuaHan * PhuPhiMoiGio).ToString("N0");
                    }
                    else
                    {
                        SoGioQuaHan = 0;
                        _phuPhiInput = "0";
                    }
                    _hoaDon = null;
                }

                // ── Tên phòng hiển thị trên Header ─────────────────────────────
                var roomNames = segments.Select(s => s.TenPhong).Distinct().ToList();
                TenPhong = roomNames.Count > 1 ? string.Join(" → ", roomNames) : roomNames.First();

                // ── Dịch vụ tổng hợp ──────────────────────────────────────────
                var allDichVu = allSegments
                    .SelectMany(ct => ct.ChiTietDichVus ?? Enumerable.Empty<ChiTietDichVu>())
                    .Where(x => x.DichVu != null)
                    .Select(x => new ChiTietDichVuDTO
                    {
                        TenDichVu = x.DichVu.TenDichVu,
                        DonGia = x.DonGia,
                        SoLuong = x.SoLuong
                    });
                DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>(allDichVu);

                NotifyAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu hóa đơn: " + ex.Message, "Lỗi");
            }
        }

        // ══════════════════════════════════════════════
        //  ExecuteCheckOut
        // ══════════════════════════════════════════════
        private void ExecuteCheckOut()
        {
            if (IsPaid) return;

            var dialog = new PaymentMethodDialog();
            if (dialog.ShowDialog() != true) return;

            int phuongThuc = dialog.SelectedMethod;
            decimal tongCuoi = TongThanhToan;

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

                // ── Lưu Hóa Đơn ─────────────────────────────────────────────
                var hoaDon = new HoaDon
                {
                    MaDatPhong = _maDatPhong,
                    MaNhanVien = _maNhanVien,
                    TongTienPhong = TongTienPhong,
                    TongTienDichVu = TongTienDichVu,
                    PhuPhi = ParsedPhuPhi,
                    TienCoc = TienCoc,
                    TongThanhToan = tongCuoi,
                    NgayThanhToan = DateTime.Now,
                    PhuongThucThanhToan = phuongThuc,
                    GhiChu = GhiChu,
                    TrangThaiThanhToan = "Đã thanh toán"
                };
                ctx.HoaDons.Add(hoaDon);

                // ── Cập nhật trạng thái đặt phòng ─────────────────────────────
                var datPhong = ctx.DatPhongs.Find(_maDatPhong);
                if (datPhong != null)
                {
                    datPhong.TrangThaiDat = 3; // Hoàn tất
                    if (datPhong.TrangThaiCoc == 0)
                        datPhong.TrangThaiCoc = 2; // Đã khấu trừ/hoàn thành cọc
                }

                // ── Log lịch sử cọc nếu có ────────────────────────────────────
                if (TienCoc > 0 && !_depositAlreadyApplied)
                {
                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = _maDatPhong,
                        LoaiGiaoDich = 2,
                        SoTien = TienCoc,
                        ThoiGian = DateTime.Now,
                        MaNhanVien = _maNhanVien,
                        GhiChu = $"Khấu trừ cọc khi checkout {TenPhong}. " +
                                 $"Tổng trước cọc: {TongTienPhong + TongTienDichVu + ParsedPhuPhi:#,0}₫. " +
                                 $"Thực thu: {tongCuoi:#,0}₫."
                    });
                }

                // ── Trả phòng đang hoạt động (Active segment) ──────────────────
                var activeChiTiet = ctx.ChiTietDatPhongs.Find(_maChiTietDatPhongActive);
                if (activeChiTiet != null)
                {
                    activeChiTiet.NgayCheckOut = NgayCheckOut;

                    var activePhong = ctx.Phongs.Find(activeChiTiet.MaPhong);
                    if (activePhong != null)
                    {
                        activePhong.TrangThai = 0;       // Trống
                        activePhong.TrangThaiDonDep = 1; // Cần dọn dẹp
                    }
                }

                ctx.SaveChanges();

                // ── Cập nhật Local UI State ─────────────────────────────────────
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
            OnPropertyChanged(nameof(SoDemText));
            OnPropertyChanged(nameof(DanhSachSegment));
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
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
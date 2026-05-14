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
        // ══════════════════════════════════════════════
        //  Internal state
        // ══════════════════════════════════════════════
        private readonly int _maDatPhong;
        private readonly int _maNhanVien;
        private readonly int _maChiTietDatPhong;

        private HoaDon _hoaDon;

        // ══════════════════════════════════════════════
        //  Booking Info  (read-only display)
        // ══════════════════════════════════════════════
        public int MaDatPhong { get; private set; }
        public string MaHoaDonText => _hoaDon != null ? $"#HD-{_hoaDon.MaHoaDon:D6}" : "Chưa lập";
        public string TenKhachHang { get; private set; }
        public string SdtKhachHang { get; private set; }
        public string TenPhong { get; private set; }
        public string TenNhanVien { get; private set; }
        public DateTime NgayCheckIn { get; private set; }
        public DateTime NgayCheckOut { get; private set; }

        /// <summary>Ngày check-out theo hợp đồng ban đầu (trước khi quá hạn).</summary>
        public DateTime NgayCheckOutHopDong { get; private set; }

        public decimal GiaDatMoiDem { get; private set; }

        // ══════════════════════════════════════════════
        //  Overdue calculation
        // ══════════════════════════════════════════════

        /// <summary>
        /// Số giờ vượt quá so với NgayCheckOutHopDong tại thời điểm checkout thực tế.
        /// Chỉ > 0 khi NgayCheckOut (thực) > NgayCheckOutHopDong.
        /// </summary>
        public int SoGioQuaHan { get; private set; }

        /// <summary>Phụ phí mỗi giờ thêm từ LoaiPhong.</summary>
        public decimal PhuPhiMoiGio { get; private set; }

        /// <summary>Chuỗi hiển thị số giờ quá hạn, ẩn khi không quá hạn.</summary>
        public string SoGioQuaHanText =>
            SoGioQuaHan > 0
                ? $"⚠️  Quá hạn {SoGioQuaHan} giờ  ×  {PhuPhiMoiGio:#,0}₫/giờ  =  {SoGioQuaHan * PhuPhiMoiGio:#,0}₫"
                : "";

        public bool HasOverdue => SoGioQuaHan > 0;

        // ══════════════════════════════════════════════
        //  Số đêm — split thành đêm hợp đồng + đêm gia hạn
        // ══════════════════════════════════════════════

        /// <summary>Số đêm trong hợp đồng gốc (CheckIn → CheckOut hợp đồng).</summary>
        public int SoDemHopDong =>
            Math.Max(1, (int)Math.Ceiling((NgayCheckOutHopDong - NgayCheckIn).TotalDays));

        /// <summary>
        /// Số đêm gia hạn thêm (từ NgayCheckOutHopDong → NgayCheckOut thực tế).
        /// Chỉ tính khi đã gia hạn và checkout thực tế vượt qua ngày hợp đồng.
        /// </summary>
        public int SoDemGiaHan =>
            NgayCheckOut.Date > NgayCheckOutHopDong.Date
                ? (int)Math.Ceiling((NgayCheckOut - NgayCheckOutHopDong).TotalDays)
                : 0;

        //public int SoDem => SoDemHopDong + SoDemGiaHan;
        private int _soDem;
        public int SoDem
        {
            get => _soDem;
            set { _soDem = value; OnPropertyChanged(); }
        }

        public string SoDemText => $"({SoDem} đêm × {GiaDatMoiDem:#,0}₫)";
        //public string SoDemText
        //{
        //    get
        //    {
        //        if (SoDemGiaHan > 0)
        //            return $"({SoDemHopDong} đêm hợp đồng + {SoDemGiaHan} đêm gia hạn) × {GiaDatMoiDem:#,0}₫";
        //        return $"({SoDem} đêm × {GiaDatMoiDem:#,0}₫)";
        //    }
        //}

        // ══════════════════════════════════════════════
        //  Editable field: PhuPhi
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
        //  Charge properties
        // ══════════════════════════════════════════════
        public decimal TongTienPhong => GiaDatMoiDem * SoDem;
        public decimal TongTienDichVu => DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0;
        public decimal TienCoc { get; private set; }
        public decimal TongThanhToan => TongTienPhong + TongTienDichVu + ParsedPhuPhi - TienCoc;

        public string TongTienPhongText => $"{TongTienPhong:#,0}₫";
        public string TongTienDichVuText => $"{TongTienDichVu:#,0}₫";
        public string PhuPhiText => $"{ParsedPhuPhi:#,0}₫";
        public string TienCocText => $"- {TienCoc:#,0}₫";
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
        //  Service list
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
        //  Data load
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

                var chiTiet = _maChiTietDatPhong > 0
                    ? datPhong.ChiTietDatPhongs
                        .FirstOrDefault(ct => ct.MaChiTietDatPhong == _maChiTietDatPhong)
                    : datPhong.ChiTietDatPhongs.FirstOrDefault();

                if (chiTiet != null)
                {
                    TenPhong = chiTiet.Phong?.TenPhong ?? "—";
                    NgayCheckIn = chiTiet.NgayCheckIn;
                    NgayCheckOut = DateTime.Now;           // thời điểm checkout thực tế
                    NgayCheckOutHopDong = chiTiet.NgayCheckOut;   // deadline trong hợp đồng
                    GiaDatMoiDem = chiTiet.GiaDat;
                    PhuPhiMoiGio = chiTiet.Phong?.LoaiPhong?.PhuPhiThemGio ?? 0;

                    // 2. Tính số đêm thực tế (Tối thiểu 1)
                    int soDemThucTe = Math.Max(1, (int)Math.Ceiling((NgayCheckOut - NgayCheckIn).TotalDays));

                    // ── Tính phụ phí quá hạn ─────────────────────────────────
                    // Quá hạn = checkout thực > deadline hợp đồng VÀ chưa gia hạn
                    // (tức NgayCheckOut thực tế <= NgayCheckOutHopDong nghĩa là đã gia hạn
                    //  hoặc checkout đúng hạn → phụ phí = 0)
                    //if (NgayCheckOut > NgayCheckOutHopDong && SoDemGiaHan == 0)
                    //{
                    //    // Quá hạn thật sự: tính số giờ vượt deadline
                    //    double totalLateHours = (NgayCheckOut - NgayCheckOutHopDong).TotalHours;
                    //    SoGioQuaHan = (int)Math.Floor(totalLateHours);

                    //    _phuPhiInput = SoGioQuaHan > 0
                    //        ? (SoGioQuaHan * PhuPhiMoiGio).ToString("N0")
                    //        : "0";
                    //}
                    //else
                    //{
                    //    SoGioQuaHan = 0;
                    //    _phuPhiInput = "0";
                    //}
                    // 3. Kiểm tra logic Gia hạn & Quá hạn
                    // Giả sử bạn có cách nhận biết đã gia hạn (Ví dụ: So với ngày check-out mặc định ban đầu)
                    // Hoặc đơn giản là theo Plan:

                    if (NgayCheckOut <= NgayCheckOutHopDong)
                    {
                        // TRƯỜNG HỢP 1: Check-out bình thường (Sớm hoặc Đúng hạn)
                        SoDem = soDemThucTe;
                        SoGioQuaHan = 0;
                        _phuPhiInput = "0";
                    }
                    else
                    {
                        // Kiểm tra xem đây là "Gia hạn" hay "Quá hạn không phép"
                        // Logic: Nếu khách đã làm thủ tục gia hạn, NgayCheckOutHopDong sẽ >= NgayCheckOut thực tế
                        // Nếu NgayCheckOut thực tế đã vượt cả NgayCheckOutHopDong thì tính là Quá hạn.

                        bool isGiaHan = false;
                        // Note: Nếu bạn có field 'IsGiaHan' trong DB thì check ở đây. 
                        // Nếu không, logic "đã gia hạn thì số đêm tính toàn bộ thực tế" 
                        // sẽ khớp với TRƯỜNG HỢP 1 ở trên khi nhân viên đã dời NgayCheckOutHopDong đi.

                        if (!isGiaHan)
                        {
                            // TRƯỜNG HỢP 2: Quá hạn không gia hạn
                            // Tính số đêm theo hợp đồng
                            SoDem = Math.Max(1, (int)Math.Ceiling((NgayCheckOutHopDong - NgayCheckIn).TotalDays));

                            // Tính phụ phí giờ (Làm tròn xuống)
                            double totalLateHours = (NgayCheckOut - NgayCheckOutHopDong).TotalHours;
                            SoGioQuaHan = (int)Math.Floor(totalLateHours);

                            _phuPhiInput = (SoGioQuaHan * PhuPhiMoiGio).ToString("N0");
                        }
                        else
                        {
                            // TRƯỜNG HỢP 3: Đã gia hạn (NgayCheckOutHopDong mới > cũ)
                            SoDem = soDemThucTe;
                            SoGioQuaHan = 0;
                            _phuPhiInput = "0";
                        }
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

                // Kiểm tra hóa đơn đã thanh toán chưa
                _hoaDon = ctx.HoaDons.FirstOrDefault(h => h.MaDatPhong == _maDatPhong);
                if (_hoaDon != null && _hoaDon.TrangThaiThanhToan == "Đã thanh toán")
                {
                    _isPaid = true;
                    _phuPhiInput = _hoaDon.PhuPhi.ToString();
                    GhiChu = _hoaDon.GhiChu ?? "";
                    _phuongThucThanhToan = _hoaDon.PhuongThucThanhToan;
                    PhuongThucThanhToanText = ToPaymentLabel(_hoaDon.PhuongThucThanhToan);
                    _ngayThanhToanText = $"Ngày TT: {_hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";

                    var maPhongs = datPhong.ChiTietDatPhongs.Select(ct => ct.MaPhong).ToList();
                    var phongs = ctx.Phongs.Where(p => maPhongs.Contains(p.MaPhong) && p.TrangThai != 0).ToList();
                    foreach (var p in phongs)
                    {
                        p.TrangThai = 0;
                        p.TrangThaiDonDep = 1;
                    }
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
        //  CheckOut
        // ══════════════════════════════════════════════
        private void ExecuteCheckOut()
        {
            if (IsPaid) return;

            var dialog = new PaymentMethodDialog();
            if (dialog.ShowDialog() != true) return;

            int phuongThuc = dialog.SelectedMethod;

            var tongCuoi = TongThanhToan;
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

                var datPhong = ctx.DatPhongs.Find(_maDatPhong);
                if (datPhong != null) datPhong.TrangThaiDat = 3;

                var maPhongs = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .Select(ct => ct.MaPhong)
                    .ToList();

                var phongList = ctx.Phongs.Where(p => maPhongs.Contains(p.MaPhong)).ToList();
                foreach (var p in phongList)
                {
                    p.TrangThai = 0;
                    p.TrangThaiDonDep = 1;
                }

                var chiTietRows = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .ToList();
                foreach (var ct in chiTietRows)
                    ct.NgayCheckOut = NgayCheckOut;

                ctx.SaveChanges();

                _hoaDon = hoaDon;
                _phuongThucThanhToan = phuongThuc;
                PhuongThucThanhToanText = ToPaymentLabel(phuongThuc);
                _ngayThanhToanText = $"Ngày TT: {hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";
                IsPaid = true;

                NotifyAll();
                MessageBox.Show("Thanh toán thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
            OnPropertyChanged(nameof(TongTienPhong));
            OnPropertyChanged(nameof(TongTienPhongText));
            OnPropertyChanged(nameof(TongTienDichVuText));
            OnPropertyChanged(nameof(PhuPhiText));
            OnPropertyChanged(nameof(TienCocText));
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
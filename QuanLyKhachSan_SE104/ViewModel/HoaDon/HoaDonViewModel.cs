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
        private readonly int _maNhanVien;          // logged-in staff ID — pass from parent VM
        private readonly int _maChiTietDatPhong;   // needed to read services

        private HoaDon _hoaDon;                    // null until checkout is confirmed

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
        public int SoDem => Math.Max(1, (int)Math.Ceiling((NgayCheckOut - NgayCheckIn).TotalDays));
        public string SoDemText => $"({SoDem} đêm × {GiaDatMoiDem:#,0}₫)";

        // Price per night from ChiTietDatPhong.GiaDat
        public decimal GiaDatMoiDem { get; private set; }

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
                // Recompute total whenever surcharge changes
                OnPropertyChanged(nameof(PhuPhiText));
                OnPropertyChanged(nameof(TongThanhToanText));
                OnPropertyChanged(nameof(TongThanhToan));
            }
        }

        //private decimal ParsedPhuPhi =>
        //    decimal.TryParse(_phuPhiInput.Replace(",", "").Replace(".", ""), out var v) && v >= 0 ? v : 0;
        private decimal ParsedPhuPhi
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_phuPhiInput)) return 0;
                // Loại bỏ dấu phân cách nghìn nếu có và parse
                string cleanInput = _phuPhiInput.Replace(",", "").Replace(".", "").Trim();
                return decimal.TryParse(cleanInput, out var v) ? v : 0;
            }
        }

        // ══════════════════════════════════════════════
        //  Charge properties
        // ══════════════════════════════════════════════
        public decimal TongTienPhong => GiaDatMoiDem * SoDem;
        public decimal TongTienDichVu => DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0;
        public decimal TienCoc { get; private set; }
        public decimal TongThanhToan => TongTienPhong + TongTienDichVu + ParsedPhuPhi - TienCoc;

        // Formatted strings for binding
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
        //  GhiChu (note field)
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
        //  Pass maDatPhong, maChiTietDatPhong, maNhanVien
        //  from ChiTietPhongViewModel or wherever checkout is triggered.
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

                // Load booking + customer + staff + room details + services
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

                // Booking info
                MaDatPhong = datPhong.MaDatPhong;
                TenKhachHang = datPhong.KhachHang?.HoTen ?? "—";
                SdtKhachHang = datPhong.KhachHang?.SDT ?? "—";
                TenNhanVien = datPhong.NhanVien?.HoTen ?? "—";
                TienCoc = datPhong.TienCoc;

                // Find the specific ChiTietDatPhong for this room
                var chiTiet = _maChiTietDatPhong > 0
                    ? datPhong.ChiTietDatPhongs
                        .FirstOrDefault(ct => ct.MaChiTietDatPhong == _maChiTietDatPhong)
                    : datPhong.ChiTietDatPhongs.FirstOrDefault();

                if (chiTiet != null)
                {
                    TenPhong = chiTiet.Phong?.TenPhong ?? "—";
                    NgayCheckIn = chiTiet.NgayCheckIn;
                    NgayCheckOut = DateTime.Now;
                    GiaDatMoiDem = chiTiet.GiaDat;
                    var deadline = chiTiet.NgayCheckOut;

                    if (NgayCheckOut > deadline && chiTiet.Phong?.LoaiPhong != null)
                    {
                        double totalLateHours = (NgayCheckOut - deadline).TotalHours;
                        int soGioTinhTien = (int)Math.Floor(totalLateHours);

                        if (soGioTinhTien > 0)
                        {
                            decimal donGiaPhuPhi = chiTiet.Phong.LoaiPhong.PhuPhiThemGio;
                            _phuPhiInput = (soGioTinhTien * donGiaPhuPhi).ToString("N0");
                        }
                        else _phuPhiInput = "0";
                    }
                    else
                    {
                        _phuPhiInput = "0"; // Chưa quá giờ dự kiến thì phụ phí = 0
                    }

                    // Services for this room stay
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

                // Check if invoice already exists (re-opened after payment).
                // Dùng HoaDon.TrangThaiThanhToan làm nguồn sự thật duy nhất —
                // KHÔNG dùng DatPhong.TrangThaiDat vì số 3 bị dùng cho cả
                // "Đã trả phòng" (DatPhong) lẫn "Quá hạn" (Phong), rất dễ nhầm.
                _hoaDon = ctx.HoaDons.FirstOrDefault(h => h.MaDatPhong == _maDatPhong);
                if (_hoaDon != null && _hoaDon.TrangThaiThanhToan == "Đã thanh toán")
                {
                    _isPaid = true;
                    _phuPhiInput = _hoaDon.PhuPhi.ToString();
                    GhiChu = _hoaDon.GhiChu ?? "";
                    _phuongThucThanhToan = _hoaDon.PhuongThucThanhToan;
                    PhuongThucThanhToanText = ToPaymentLabel(_hoaDon.PhuongThucThanhToan);
                    _ngayThanhToanText = $"Ngày TT: {_hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";

                    // Đảm bảo các phòng của hoá đơn đã thanh toán được reset về Trống
                    var maPhongs = datPhong.ChiTietDatPhongs.Select(ct => ct.MaPhong).ToList();
                    var phongs = ctx.Phongs.Where(p => maPhongs.Contains(p.MaPhong) && p.TrangThai != 0).ToList();
                    foreach (var p in phongs)
                    {
                        p.TrangThai = 0;       // Trống
                        p.TrangThaiDonDep = 2; // Cần dọn
                    }
                    if (phongs.Any()) ctx.SaveChanges();
                }
                else
                {
                    // Chưa thanh toán (hoặc HoaDon orphan chưa hoàn tất)
                    _isPaid = false;
                    _hoaDon = null;
                }

                // Notify all computed display props
                NotifyAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu hóa đơn: " + ex.Message, "Lỗi");
            }
        }


        // ══════════════════════════════════════════════
        //  CheckOut — shows payment method picker, then saves HoaDon
        // ══════════════════════════════════════════════
        private void ExecuteCheckOut()
        {
            if (IsPaid) return;

            // ── Payment method dialog ──────────────────
            var dialog = new PaymentMethodDialog();
            if (dialog.ShowDialog() != true) return;   // user cancelled

            int phuongThuc = dialog.SelectedMethod;    // 0=Tiền mặt, 1=Thẻ, 2=Chuyển khoản

            // ── Confirm total ──────────────────────────
            var tongCuoi = TongThanhToan;
            var confirm = MessageBox.Show(
                $"Phương thức: {ToPaymentLabel(phuongThuc)}\n" +
                $"Tổng thanh toán: {tongCuoi:#,0}₫\n\n" +
                "Xác nhận hoàn tất thanh toán?",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            // ── Persist ────────────────────────────────
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

                // Mark booking as checked-out
                var datPhong = ctx.DatPhongs.Find(_maDatPhong);
                if (datPhong != null) datPhong.TrangThaiDat = 3;

                // Mark room(s) as empty + needs cleaning — query Phong directly
                //var chiTietList = ctx.ChiTietDatPhongs
                //    .Where(ct => ct.MaDatPhong == _maDatPhong)
                //    .Select(ct => ct.MaPhong)
                //    .ToList();

                //var phongList = ctx.Phongs
                //    .Where(p => chiTietList.Contains(p.MaPhong))
                //    .ToList();
                // 3. QUAN TRỌNG: Tìm tất cả các phòng thuộc đơn đặt này và đưa về Trống (0)
                var maPhongs = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .Select(ct => ct.MaPhong)
                    .ToList();

                var phongList = ctx.Phongs.Where(p => maPhongs.Contains(p.MaPhong)).ToList();

                foreach (var p in phongList)
                {
                    p.TrangThai = 0;       // Trống
                    p.TrangThaiDonDep = 2; // Cần dọn
                }

                // Update checkout timestamp on ChiTietDatPhong rows
                var chiTietRows = ctx.ChiTietDatPhongs
                    .Where(ct => ct.MaDatPhong == _maDatPhong)
                    .ToList();
                foreach (var ct in chiTietRows)
                    ct.NgayCheckOut = NgayCheckOut;

                ctx.SaveChanges();

                // Update local state
                _hoaDon = hoaDon;
                _phuongThucThanhToan = phuongThuc;
                PhuongThucThanhToanText = ToPaymentLabel(phuongThuc);
                _ngayThanhToanText = $"Ngày TT: {hoaDon.NgayThanhToan:dd/MM/yyyy HH:mm}";
                IsPaid = true;

                NotifyAll();
                MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("Lỗi khi lưu hóa đơn: " + msg, "Lỗi");
            }
        }

        private void ExecuteInHoaDon()
        {
            // TODO: integrate with a print/PDF library
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
            OnPropertyChanged(nameof(SoDem));
            OnPropertyChanged(nameof(SoDemText));
            OnPropertyChanged(nameof(GiaDatMoiDem));
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

        // ── INotifyPropertyChanged ────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
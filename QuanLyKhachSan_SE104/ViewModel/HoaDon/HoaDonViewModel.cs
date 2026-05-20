using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Services;
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
        private readonly IHoaDonService _hoaDonService;

        private string _maHoaDonText = "Chưa lập";
        private int _maChiTietDatPhongActive;

        private bool _overdueInitialized = false;
        private int _soGioQuaHanLocked = 0;

        public Action CloseAction { get; set; }

        // ══════════════════════════════════════════════
        //  Booking header info (read-only display)
        // ══════════════════════════════════════════════
        public int MaDatPhong { get; private set; }
        public string MaHoaDonText => _maHoaDonText;
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

        public decimal PhuPhi => _phuPhi;

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
        //  Deposit
        // ══════════════════════════════════════════════
        public decimal TienCoc { get; private set; }
        private bool _depositAlreadyApplied = false;

        // ══════════════════════════════════════════════
        //  Charge totals
        // ══════════════════════════════════════════════
        private decimal _tongTienPhong;
        private decimal _tongTienDichVu;
        private decimal _phuPhi;
        private decimal _tongThanhToan;

        public decimal TongTienPhong => _tongTienPhong;
        public decimal TongTienDichVu => _tongTienDichVu;
        public decimal TongThanhToan => _tongThanhToan;

        public string TongTienPhongText => $"{TongTienPhong:#,0}₫";
        public string TongTienDichVuText => $"{TongTienDichVu:#,0}₫";
        public string PhuPhiText => $"{_phuPhi:#,0}₫";
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
        public HoaDonViewModel(int maDatPhong, int maChiTietDatPhong, IHoaDonService? hoaDonService = null)
        {
            _maDatPhong = maDatPhong;
            _maChiTietDatPhong = maChiTietDatPhong;
            _maNhanVien = LoginSession.CurrentNhanVienId;
            _hoaDonService = hoaDonService ?? new HoaDonService();

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
                var invoice = _hoaDonService.GetInvoiceDetails(_maDatPhong, _maChiTietDatPhong, _maNhanVien);
                ApplyInvoiceDetails(invoice);
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

            var confirm = MessageBox.Show(
                $"Phương thức: {ToPaymentLabel(phuongThuc)}\n" +
                "Xác nhận hoàn tất thanh toán?",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var invoice = _hoaDonService.ProcessCheckOut(new CheckOutRequestDTO
                {
                    MaDatPhong = _maDatPhong,
                    MaChiTietDatPhongActive = _maChiTietDatPhongActive,
                    MaNhanVien = _maNhanVien,
                    PhuongThucThanhToan = phuongThuc,
                    GhiChu = GhiChu
                });
                ApplyInvoiceDetails(invoice);

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

        private void ApplyInvoiceDetails(InvoiceDetailDTO invoice)
        {
            MaDatPhong = invoice.MaDatPhong;
            _maChiTietDatPhongActive = invoice.MaChiTietDatPhongActive;
            _maHoaDonText = invoice.MaHoaDonText;
            TenKhachHang = invoice.TenKhachHang;
            SdtKhachHang = invoice.SdtKhachHang;
            TenNhanVien = invoice.TenNhanVien;
            TenPhong = invoice.TenPhong;
            NgayCheckIn = invoice.NgayCheckIn;
            NgayCheckOut = invoice.NgayCheckOut;
            NgayCheckOutHopDong = invoice.NgayCheckOutHopDong;
            DanhSachSegment = new ObservableCollection<PhongSegmentDTO>(invoice.DanhSachSegment);
            SoGioQuaHan = invoice.SoGioQuaHan;
            PhuPhiMoiGio = invoice.PhuPhiMoiGio;
            TienCoc = invoice.TienCoc;
            _depositAlreadyApplied = invoice.DepositAlreadyApplied;
            _tongTienPhong = invoice.TongTienPhong;
            _tongTienDichVu = invoice.TongTienDichVu;
            _phuPhi = invoice.PhuPhi;
            _tongThanhToan = invoice.TongThanhToan;
            _phuongThucThanhToan = invoice.PhuongThucThanhToan;
            PhuongThucThanhToanText = invoice.PhuongThucThanhToanText;
            _ngayThanhToanText = invoice.NgayThanhToanText;
            DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>(invoice.DanhSachDichVu);
            GhiChu = invoice.GhiChu;
            IsPaid = invoice.IsPaid;

            NotifyAll();
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
            OnPropertyChanged(nameof(TongTienDichVu));
            OnPropertyChanged(nameof(TongTienDichVuText));
            OnPropertyChanged(nameof(PhuPhi));
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

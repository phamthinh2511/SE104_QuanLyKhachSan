using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.DatPhong;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ChiTietDatPhongModel = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;
using PhongModel = QuanLyKhachSan_SE104.Model.Phong;
using QuanLyKhachSan_SE104.Services;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM
{
    public class ChiTietPhongViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly Window _window;

        private ObservableCollection<ChiTietDichVuDTO> _danhSachDichVu = new();
        public ObservableCollection<ChiTietDichVuDTO> DanhSachDichVu
        {
            get => _danhSachDichVu;
            set { _danhSachDichVu = value; OnPropertyChanged(); OnPropertyChanged(nameof(TongTienDichVuText)); }
        }

        public PhongModel Phong { get; }
        public ChiTietDatPhongModel ChiTietDatPhong { get; }

        public int SoDem
        {
            get
            {
                if (ChiTietDatPhong == null) return 0;
                double totalDays = (ChiTietDatPhong.NgayCheckOut - ChiTietDatPhong.NgayCheckIn).TotalDays;
                return Math.Max(1, (int)Math.Ceiling(totalDays));
            }
        }

        public string TongTienDichVuText
            => $"{DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0:#,0}₫";

        private readonly DichVuDAL _dichVuDal = new();


        // ════════════════════════════════════════════════════════════════
        //  Commands
        // ════════════════════════════════════════════════════════════════

        public ICommand ThoatCommand => new RelayCommand(() => _window.Close());

        // ── Walk-in check-in ──────────────────────────────────────────────────
        public ICommand CheckInKhachLeCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null) return;

            var vm = new DatPhongViewModel(phong);
            var page = new DatPhongPage { DataContext = vm };
            var win = new Window
            {
                Title = $"Check-in khách lẻ — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };
            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true)
            {
                using var ctx = new QuanLyKhachSanContext();
                var refreshedPhong = ctx.Phongs.Find(phong.MaPhong);
                if (refreshedPhong != null)
                {
                    phong.TrangThai = refreshedPhong.TrangThai;
                    phong.TrangThaiDonDep = refreshedPhong.TrangThaiDonDep;
                    OnPropertyChanged(nameof(Phong));
                }

                _window.Close();
            }
        });

        // ── Check-in for confirmed booking ────────────────────────────────────
        public ICommand CheckInDaDatCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();

                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p != null) p.TrangThai = 2;

                var dat = ctx.DatPhongs.Find(ChiTietDatPhong.MaDatPhong);
                if (dat != null) dat.TrangThaiDat = 2;

                var ct = ctx.ChiTietDatPhongs.Find(ChiTietDatPhong.MaChiTietDatPhong);
                if (ct != null)
                {
                    ct.NgayCheckIn = DateTime.Now;
                    ct.TrangThaiSegment = TrangThaiSegment.DangO;
                    ChiTietDatPhong.NgayCheckIn = DateTime.Now;
                    ChiTietDatPhong.TrangThaiSegment = TrangThaiSegment.DangO;
                }

                ctx.SaveChanges();
                try
                {
                    var hdService = new HoaDonService();
                    hdService.GetInvoiceDetails(ChiTietDatPhong.MaDatPhong, ChiTietDatPhong.MaChiTietDatPhong, LoginSession.CurrentNhanVienId);
                }
                catch (Exception){}

                RefreshDichVu();
                OnPropertyChanged(string.Empty);
                MessageBox.Show($"Check-in thành công phòng {phong.TenPhong}!", "Thông báo");
                HotelEventBus.PublishRoomStatusChanged();
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi check-in: " + ex.Message); }
        });

        // ── Hủy đặt phòng — Rule 01 vs Rule 02 ──────────────────────────────
        /// <summary>
        /// Presents a two-option dialog so staff can choose:
        ///   • Timely cancellation  → TrangThaiDat = 4, TrangThaiCoc = 1 (refund)
        ///   • No-show / late cancel → TrangThaiDat = 5, TrangThaiCoc = 2 (forfeit)
        /// Both paths write a LichSuCoc audit row.
        /// </summary>
        public ICommand HuyDatPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            var choiceResult = MessageBox.Show(
                $"Hủy đặt phòng {phong.TenPhong}\n\n" +
                "Chọn loại hủy:\n" +
                "• [Yes]  Hủy đúng hạn  → Hoàn 100% tiền cọc\n" +
                "• [No]   No-show / trễ  → Thu tiền cọc vào doanh thu",
                "Chọn loại hủy",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choiceResult == MessageBoxResult.Cancel) return;

            bool isTimelyCancel = (choiceResult == MessageBoxResult.Yes);

            string confirmMsg = isTimelyCancel
                ? $"Xác nhận HỦY ĐÚNG HẠN phòng {phong.TenPhong}?\nTiền cọc sẽ được HOÀN TRẢ cho khách."
                : $"Xác nhận NO-SHOW / HỦY TRỄ phòng {phong.TenPhong}?\nTiền cọc sẽ được GHI VÀO DOANH THU.";

            if (MessageBox.Show(confirmMsg, "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new QuanLyKhachSanContext();

                var dat = ctx.DatPhongs
                    .Include(d => d.ChiTietDatPhongs)
                    .FirstOrDefault(d => d.MaDatPhong == ChiTietDatPhong.MaDatPhong);

                if (dat == null) return;

                // ── Ràng buộc: chỉ có thể hủy đơn đặt phòng ở trạng thái 1 (đã xác nhận, chưa check-in) ──
                if (dat.TrangThaiDat != 1)
                {
                    MessageBox.Show(
                        "Chỉ có thể hủy đặt phòng ở trạng thái 'Đã xác nhận'.\n" +
                        "Khách đang ở hoặc đã trả phòng không thể hủy theo quy trình này.",
                        "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── Ràng buộc: TrangThaiCoc chưa từng được xử lý trước đó ──────────────
                if (dat.TrangThaiCoc != 0)
                {
                    MessageBox.Show(
                        "Tiền cọc đã được xử lý trước đó. Kiểm tra lịch sử cọc trước khi tiếp tục.",
                        "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dat.TrangThaiDat = isTimelyCancel ? 4 : 5;
                dat.TrangThaiCoc = isTimelyCancel ? 1 : 2;

                // ── Giải phóng phòng + Đặt lại trạng thái dọn dẹp ─────────────────────
                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p != null)
                {
                    p.TrangThai = 0;
                    p.TrangThaiDonDep = 0;
                }

                var ct = ctx.ChiTietDatPhongs.Find(ChiTietDatPhong.MaChiTietDatPhong);
                if (ct != null)
                {
                    ct.NgayCheckOut = DateTime.Now;
                    ct.TrangThaiSegment = TrangThaiSegment.DaCheckOut;
                }

                // ── Ghi lịch sử kiểm toán tiền cọc ────────────────────────────────────
                if (dat.TienCoc > 0)
                {
                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = dat.MaDatPhong,
                        LoaiGiaoDich = isTimelyCancel ? 1 : 2,
                        SoTien = dat.TienCoc,
                        ThoiGian = DateTime.Now,
                        MaNhanVien = LoginSession.CurrentNhanVienId,
                        GhiChu = isTimelyCancel
                            ? $"Hủy đúng hạn phòng {phong.TenPhong} — hoàn cọc {dat.TienCoc:#,0}₫"
                            : $"No-show / hủy trễ phòng {phong.TenPhong} — thu cọc {dat.TienCoc:#,0}₫ vào doanh thu"
                    });
                }

                ctx.SaveChanges();

                string resultMsg = isTimelyCancel
                    ? $"Đã hủy phòng {phong.TenPhong}.\nTiền cọc {dat.TienCoc:#,0}₫ cần được hoàn trả cho khách."
                    : $"Đã ghi nhận no-show phòng {phong.TenPhong}.\nTiền cọc {dat.TienCoc:#,0}₫ đã chuyển vào doanh thu.";

                MessageBox.Show(resultMsg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                HotelEventBus.PublishRoomStatusChanged();
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi hủy đặt: " + (ex.InnerException?.Message ?? ex.Message)); }
        });

        // ── Room transfer (Rule 04) ────────────────────────────────────────────
        public ICommand DoiPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            var vm = new DatPhongViewModel(phong, ChiTietDatPhong);
            var page = new DatPhongPage { DataContext = vm };
            var win = new Window
            {
                Title = $"Đổi phòng — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };
            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true) _window.Close();
        });

        // ── Renew overdue room ────────────────────────────────────────────────
        public ICommand GiaHanPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            ChiTietDatPhongModel chiTietFull;
            using (var ctx = new QuanLyKhachSanContext())
            {
                chiTietFull = ctx.ChiTietDatPhongs
                    .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                    .FirstOrDefault(c => c.MaChiTietDatPhong == ChiTietDatPhong.MaChiTietDatPhong)
                    ?? ChiTietDatPhong;
            }

            var vm = new DatPhongViewModel(phong, chiTietFull, giaHan: true);
            var page = new DatPhongPage { DataContext = vm };
            var win = new Window
            {
                Title = $"Gia hạn phòng — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };
            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true) _window.Close();
        });

        // ── Cleaning status cycle ─────────────────────────────────────────────
        public ICommand DoiTrangThaiDonDepCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();
                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p == null) return;

                p.TrangThaiDonDep = p.TrangThaiDonDep switch
                {
                    0 => 1,   // Sạch → Đang dọn
                    1 => 0,   // Đang dọn → Sạch
                    2 => 0,   // Bảo trì → Sạch (manual reset)
                    _ => 0
                };
                ctx.SaveChanges();

                string label = p.TrangThaiDonDep switch
                {
                    0 => "Sạch",
                    1 => "Đang dọn",
                    2 => "Bảo trì",
                    _ => "Không xác định"
                };
                MessageBox.Show($"Đã chuyển trạng thái phòng {phong.TenPhong} sang: {label}", "Thông báo");
                HotelEventBus.PublishRoomStatusChanged();
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        });

        // ── Add service ───────────────────────────────────────────────────────
        public ICommand ThemDichVuCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (ChiTietDatPhong == null)
            {
                MessageBox.Show("Không có thông tin phòng đang ở để thêm dịch vụ.", "Thông báo");
                return;
            }

            var vm = new DichVuVM.DichVuViewModel
            {
                MaChiTietDatPhong = ChiTietDatPhong.MaChiTietDatPhong
            };
            var win = new View.DichVu.DichVuPage { DataContext = vm };
            win.Owner = _window;
            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };

            if (win.ShowDialog() == true && vm.SavedItems.Count > 0)
            {
                foreach (var item in vm.SavedItems)
                    DanhSachDichVu.Add(item);
                OnPropertyChanged(nameof(TongTienDichVuText));
            }
        });

        // ── Check-out (opens HoaDonPage) ──────────────────────────────────────
        public ICommand CheckOutCommand => new RelayCommand<PhongModel>(phong =>
        {
            var vm = new HoaDonVM.HoaDonViewModel(
                ChiTietDatPhong.MaDatPhong,
                ChiTietDatPhong.MaChiTietDatPhong);

            var page = new View.HoaDon.HoaDonPage { DataContext = vm };
            var win = new Window
            {
                Title = $"Hóa đơn — Phòng {Phong.TenPhong}",
                Width = 980,
                Height = 760,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };
            win.ShowDialog();
            _window.Close();
        });

        // ════════════════════════════════════════════════════════════════
        //  Constructor + init
        // ════════════════════════════════════════════════════════════════
        public ChiTietPhongViewModel(PhongModel phong, ChiTietDatPhongModel chiTietDatPhong, Window window)
        {
            Phong = phong;
            ChiTietDatPhong = chiTietDatPhong;
            _window = window;
            RefreshDichVu();
        }

        private void RefreshDichVu()
        {
            if (ChiTietDatPhong == null)
            {
                DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>();
                return;
            }

            var rows = _dichVuDal.LayDichVuTheoMaDatPhong(ChiTietDatPhong.MaDatPhong);
            DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>(
                rows.Where(x => x.DichVu != null)
                    .Select(x => new ChiTietDichVuDTO
                    {
                        TenDichVu = x.DichVu.TenDichVu,
                        DonGia = x.DonGia,
                        SoLuong = x.SoLuong
                    }));
        }
    }
}

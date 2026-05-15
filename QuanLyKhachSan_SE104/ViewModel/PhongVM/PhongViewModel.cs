using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.PhongView;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using PhongModel = QuanLyKhachSan_SE104.Model.Phong;
using ChiTietDatPhongModel = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM
{
    public class PhongViewModel : INotifyPropertyChanged, IDisposable
    {
        // ── TODO: replace with LoginSession.CurrentUserId ─────────────────────
        private const int STAFF_ID = 1;

        // ── Data ──────────────────────────────────────────────────────────────
        private ObservableCollection<PhongModel> _allPhongs;
        private ObservableCollection<PhongModel> _listPhong;

        public ObservableCollection<PhongModel> ListPhong
        {
            get => _listPhong;
            set { _listPhong = value; OnPropertyChanged(); }
        }

        // ── Filter combos ─────────────────────────────────────────────────────
        private ObservableCollection<int> _listTang = new();
        public ObservableCollection<int> ListTang
        {
            get => _listTang;
            set { _listTang = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LoaiPhong> _listLoaiPhong = new();
        public ObservableCollection<LoaiPhong> ListLoaiPhong
        {
            get => _listLoaiPhong;
            set { _listLoaiPhong = value; OnPropertyChanged(); }
        }

        private int? _selectedTang;
        public int? SelectedTang
        {
            get => _selectedTang;
            set { _selectedTang = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private LoaiPhong _selectedLoaiPhong;
        public LoaiPhong SelectedLoaiPhong
        {
            get => _selectedLoaiPhong;
            set { _selectedLoaiPhong = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // ── Statistics ────────────────────────────────────────────────────────
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThai == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThai == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThai == 2) ?? 0;
        public int CountQuaHan => _allPhongs?.Count(p => p.TrangThai == 3) ?? 0;
        public int CountCanDonDep => _allPhongs?.Count(p => p.TrangThaiDonDep == 1) ?? 0;
        public int CountBaoTri => _allPhongs?.Count(p => p.TrangThaiDonDep == 2) ?? 0;

        // ── Search ────────────────────────────────────────────────────────────
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ExecuteSearch(); }
        }

        // Filter token: "All" | "thue:N" | "don:N"
        private string _currentStatusFilter = "All";

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand FilterCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand AddServiceCommand { get; }
        public ICommand DoiPhongCommand { get; }
        public ICommand DoiTrangThaiDonDepCommand { get; }
        public ICommand HuyDatPhongCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand MoChiTietPhongCommand { get; }
        public ICommand ShowWarningCommand { get; }

        public PhongViewModel()
        {
            HotelEventBus.RoomStatusChanged += OnRoomStatusChanged;
            LoadData();

            FilterCommand = new RelayCommand<string>(filter =>
            {
                _currentStatusFilter = filter ?? "All";
                ApplyFilter();
            });

            CheckInCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                string loai = p.TrangThai == 1 ? "khách đã đặt" : "khách lẻ";
                MessageBox.Show($"Bắt đầu check-in {loai} cho phòng {p.TenPhong}");
            });

            CheckOutCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                MessageBox.Show($"Xử lý check-out phòng {p.TenPhong}");
            });

            AddServiceCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                var win = new QuanLyKhachSan_SE104.View.DichVu.DichVuPage();
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            DoiPhongCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                MessageBox.Show($"Đổi phòng từ {p.TenPhong}");
            });

            DoiTrangThaiDonDepCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                try
                {
                    using var ctx = new QuanLyKhachSanContext();
                    var phongDb = ctx.Phongs.Find(p.MaPhong);
                    if (phongDb == null) return;

                    int trangThaiMoi = phongDb.TrangThaiDonDep switch
                    {
                        0 => 1,
                        1 => 0,
                        2 => 0,
                        _ => 0
                    };
                    phongDb.TrangThaiDonDep = trangThaiMoi;
                    ctx.SaveChanges();

                    string label = trangThaiMoi switch
                    {
                        0 => "SẠCH",
                        1 => "ĐANG DỌN",
                        2 => "BẢO TRÌ",
                        _ => "SẠCH"
                    };
                    LoadData();
                    MessageBox.Show($"Phòng {p.TenPhong} đã chuyển sang trạng thái: {label}", "Thông báo");
                }
                catch (Exception ex) { MessageBox.Show("Lỗi cập nhật: " + ex.Message); }
            });

            HuyDatPhongCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                var result = MessageBox.Show(
                    $"Xác nhận hủy đặt phòng {p.TenPhong}?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                    MessageBox.Show($"Đã hủy đặt phòng {p.TenPhong}");
            });

            EditRoomCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                var editDialog = new PhongModal(p);
                if (editDialog.ShowDialog() == true) LoadData();
            });

            AddRoomCommand = new RelayCommand<object>(_ =>
            {
                var addDialog = new PhongModal(null);
                if (addDialog.ShowDialog() == true) LoadData();
            });

            MoChiTietPhongCommand = new RelayCommand<PhongModel>(phong =>
            {
                if (phong == null) return;
                ChiTietDatPhongModel chiTiet = null;

                if (phong.TrangThai == 1 || phong.TrangThai == 2 || phong.TrangThai == 3)
                {
                    using var ctx = new QuanLyKhachSanContext();
                    chiTiet = ctx.ChiTietDatPhongs
                        .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                        .Include(c => c.ChiTietDichVus).ThenInclude(dv => dv.DichVu)
                        .FirstOrDefault(c => c.MaPhong == phong.MaPhong
                            && (c.DatPhong.TrangThaiDat == 1
                            || c.DatPhong.TrangThaiDat == 2));
                }

                var win = new QuanLyKhachSan_SE104.View.Phong.ChiTietPhong(phong, chiTiet);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
                LoadData();
            });

            ShowWarningCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                string message = (p.IsCheckInToday, p.IsCheckOutToday) switch
                {
                    (true, true) => $"Phòng {p.TenPhong} có cả khách nhận và trả phòng trong hôm nay!",
                    (true, false) => $"Hôm nay là ngày CHECK-IN của phòng {p.TenPhong}.",
                    (false, true) => $"Hôm nay là ngày CHECK-OUT của phòng {p.TenPhong}.",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(message))
                    MessageBox.Show(message, "Thông báo lịch hẹn", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        // ════════════════════════════════════════════════════════════════
        //  LoadData — runs every time the room grid is refreshed.
        //  Three auto-transitions happen here in order:
        //    1. Overdue: TrangThaiDat 1/2 + NgayCheckOut < today  → Phong.TrangThai = 3
        //    2. No-show: TrangThaiDat = 1 + NgayCheckIn midnight passed → TrangThaiDat = 5
        //    3. Alert flags: IsCheckInToday / IsCheckOutToday set on each Phong
        // ════════════════════════════════════════════════════════════════
        public void LoadData()
        {
            using var ctx = new QuanLyKhachSanContext();
            var today = DateTime.Today;
            var now = DateTime.Now;
            bool dirty = false;

            // ── 1. Mark overdue rooms (TrangThai → 3) ────────────────────────
            // NgayCheckOut strictly before today AND booking still active (1 or 2)
            var overdueChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.NgayCheckOut < today &&
                    (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                    ct.Phong.TrangThai != 3)
                .ToList();

            foreach (var ct in overdueChiTiets)
            {
                ct.Phong.TrangThai = 3;
                dirty = true;
            }

            // ── 2. Auto no-show: passes 00:00 of the day AFTER NgayCheckIn ──
            // Business rule: if a confirmed booking (TrangThaiDat = 1) has
            // NgayCheckIn.Date < today (i.e. 00:00 of next day has passed)
            // AND the guest never checked in → mark as No-show (TrangThaiDat = 5),
            // forfeit the deposit (TrangThaiCoc = 2), free the room (TrangThai = 0).
            var noShowChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.DatPhong.TrangThaiDat == 1 &&        // still only "confirmed", never checked in
                    ct.NgayCheckIn.Date < today &&           // 00:00 next day has passed
                    ct.Phong.TrangThai == 1)                 // room still in "Đã đặt" state
                .ToList();

            foreach (var ct in noShowChiTiets)
            {
                var dat = ct.DatPhong;
                var phong = ct.Phong;

                // Transition booking → No-show
                dat.TrangThaiDat = 5;

                // Forfeit deposit → revenue
                if (dat.TienCoc > 0 && dat.TrangThaiCoc == 0)
                {
                    dat.TrangThaiCoc = 2;   // Đã thu vào doanh thu

                    ctx.LichSuCocs.Add(new LichSuCoc
                    {
                        MaDatPhong = dat.MaDatPhong,
                        LoaiGiaoDich = 2,                   // Thu doanh thu
                        SoTien = dat.TienCoc,
                        ThoiGian = now,
                        MaNhanVien = STAFF_ID,
                        GhiChu = $"Auto no-show: phòng {phong.TenPhong}, " +
                                       $"ngày nhận dự kiến {ct.NgayCheckIn:dd/MM/yyyy}. " +
                                       $"Cọc {dat.TienCoc:#,0}₫ chuyển doanh thu."
                    });
                }

                // Free the room
                phong.TrangThai = 0;

                dirty = true;
            }

            if (dirty) ctx.SaveChanges();

            // ── 3. Load all rooms with navigation properties ──────────────────
            var allPhongsFromDb = ctx.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.DatPhong)
                .OrderBy(p => p.TenPhong)
                .ToList();

            // ── 4. Set alert flags (IsCheckInToday / IsCheckOutToday) ─────────
            foreach (var phong in allPhongsFromDb)
            {
                var activeBookings = phong.ChiTietDatPhongs?
                    .Where(ct => ct.DatPhong != null
                              && ct.DatPhong.TrangThaiDat != 3   // not checked-out
                              && ct.DatPhong.TrangThaiDat != 4   // not cancelled
                              && ct.DatPhong.TrangThaiDat != 5)  // not no-show
                    .ToList();

                phong.IsCheckInToday = activeBookings?.Any(ct => ct.NgayCheckIn.Date == today) ?? false;
                phong.IsCheckOutToday = activeBookings?.Any(ct => ct.NgayCheckOut.Date == today) ?? false;
            }

            _allPhongs = new ObservableCollection<PhongModel>(allPhongsFromDb);

            // ── 5. Rebuild filter combos ──────────────────────────────────────
            var tangs = _allPhongs.Select(p => p.SoTang).Distinct().OrderBy(t => t).ToList();
            ListTang = new ObservableCollection<int> { -1 };
            foreach (var t in tangs) ListTang.Add(t);

            var loaiPhongs = ctx.LoaiPhongs.Where(lp => !lp.IsDeleted).ToList();
            ListLoaiPhong = new ObservableCollection<LoaiPhong>
            {
                new LoaiPhong { MaLoaiPhong = -1, TenLoaiPhong = "Tất cả" }
            };
            foreach (var lp in loaiPhongs) ListLoaiPhong.Add(lp);

            ListPhong = new ObservableCollection<PhongModel>(_allPhongs);
            ApplyFilter();

            // ── 6. Notify stat badges ─────────────────────────────────────────
            OnPropertyChanged(nameof(CountTatCa));
            OnPropertyChanged(nameof(CountTrong));
            OnPropertyChanged(nameof(CountDaDat));
            OnPropertyChanged(nameof(CountDangO));
            OnPropertyChanged(nameof(CountQuaHan));
            OnPropertyChanged(nameof(CountCanDonDep));
            OnPropertyChanged(nameof(CountBaoTri));

            SelectedTang = -1;
            SelectedLoaiPhong = ListLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == -1);
        }

        public void Refresh() => LoadData();

        // ── Filter + Search ───────────────────────────────────────────────────
        private void ApplyFilter()
        {
            var result = _allPhongs.AsEnumerable();

            switch (_currentStatusFilter)
            {
                case "thue:0": result = result.Where(p => p.TrangThai == 0); break;
                case "thue:1": result = result.Where(p => p.TrangThai == 1); break;
                case "thue:2": result = result.Where(p => p.TrangThai == 2); break;
                case "thue:3": result = result.Where(p => p.TrangThai == 3); break;
                case "don:1": result = result.Where(p => p.TrangThaiDonDep == 1); break;
                case "don:2": result = result.Where(p => p.TrangThaiDonDep == 2); break;
            }

            if (SelectedTang.HasValue && SelectedTang.Value != -1)
                result = result.Where(p => p.SoTang == SelectedTang.Value);

            if (SelectedLoaiPhong != null && SelectedLoaiPhong.MaLoaiPhong != -1)
                result = result.Where(p => p.MaLoaiPhong == SelectedLoaiPhong.MaLoaiPhong);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var kw = SearchText.Trim().ToLower();
                result = result.Where(p => p.TenPhong?.ToLower().Contains(kw) == true);
            }

            ListPhong = new ObservableCollection<PhongModel>(result.OrderBy(p => p.TenPhong));
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListPhong = new ObservableCollection<PhongModel>(_allPhongs);
                return;
            }
            var kw = SearchText.Trim().ToLower();
            ListPhong = new ObservableCollection<PhongModel>(
                _allPhongs.Where(p => p.TenPhong?.ToLower().Contains(kw) == true));
        }
        private void OnRoomStatusChanged()
        {
            Application.Current.Dispatcher.Invoke(LoadData);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public void Dispose()
        {
            HotelEventBus.RoomStatusChanged -= OnRoomStatusChanged;
        }
    }
}
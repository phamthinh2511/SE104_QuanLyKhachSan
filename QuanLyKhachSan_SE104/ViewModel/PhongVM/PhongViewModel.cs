using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Services;
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
        // ── Data ──────────────────────────────────────────────────────────────
        private ObservableCollection<PhongModel> _allPhongs;
        private ObservableCollection<PhongModel> _listPhong;
        private IReadOnlyList<PhongDisplayDTO> _displayRooms = Array.Empty<PhongDisplayDTO>();
        private readonly IBookingQueryService _bookingQueryService = new BookingQueryService();
        private readonly RoomService _roomService = new();
        private readonly IStatusTransitionService _statusTransitionService = new StatusTransitionService();

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
            _statusTransitionService.RunDailyTransitions();
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
                    var trangThaiMoi = _roomService.ToggleCleaningStatus(p.MaPhong);
                    if (!trangThaiMoi.HasValue) return;

                    string label = trangThaiMoi.Value switch
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

                if (phong.TrangThai == 1)
                    chiTiet = _bookingQueryService.GetRoomDetailBySegment(phong.MaPhong, TrangThaiSegment.ChoNhanPhong);
                else if (phong.TrangThai == 2 || phong.TrangThai == 3)
                    chiTiet = _bookingQueryService.GetActiveRoomDetail(phong.MaPhong);

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
        //  LoadData — read-only room grid refresh.
        //  Daily state mutations are handled by IStatusTransitionService.
        // ════════════════════════════════════════════════════════════════
        public void LoadData()
        {
            _displayRooms = _bookingQueryService.GetAllRoomsForDisplay();
            _allPhongs = new ObservableCollection<PhongModel>(_displayRooms.Select(ToPhongModel));

            ListPhong = new ObservableCollection<PhongModel>(_allPhongs);
            RebuildFilterCombos();
            ApplyFilter();
            NotifyStatBadges();
        }

        private void RebuildFilterCombos()
        {
            var currentTang = SelectedTang;
            var currentLoaiPhongId = SelectedLoaiPhong?.MaLoaiPhong;

            ListTang = new ObservableCollection<int> { -1 };
            foreach (var tang in _displayRooms.Select(p => p.SoTang).Distinct().OrderBy(t => t))
                ListTang.Add(tang);

            ListLoaiPhong = new ObservableCollection<LoaiPhong>
            {
                new LoaiPhong { MaLoaiPhong = -1, TenLoaiPhong = "Tất cả" }
            };

            foreach (var loaiPhong in _displayRooms
                .GroupBy(p => new { p.MaLoaiPhong, p.TenLoaiPhong })
                .OrderBy(g => g.Key.TenLoaiPhong)
                .Select(g => new LoaiPhong
                {
                    MaLoaiPhong = g.Key.MaLoaiPhong,
                    TenLoaiPhong = g.Key.TenLoaiPhong
                }))
            {
                ListLoaiPhong.Add(loaiPhong);
            }

            SelectedTang = currentTang.HasValue && ListTang.Contains(currentTang.Value)
                ? currentTang
                : -1;
            SelectedLoaiPhong = ListLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == currentLoaiPhongId)
                ?? ListLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == -1);
        }

        private void NotifyStatBadges()
        {
            OnPropertyChanged(nameof(CountTatCa));
            OnPropertyChanged(nameof(CountTrong));
            OnPropertyChanged(nameof(CountDaDat));
            OnPropertyChanged(nameof(CountDangO));
            OnPropertyChanged(nameof(CountQuaHan));
            OnPropertyChanged(nameof(CountCanDonDep));
            OnPropertyChanged(nameof(CountBaoTri));
        }

        private static PhongModel ToPhongModel(PhongDisplayDTO dto)
        {
            return new PhongModel
            {
                MaPhong = dto.MaPhong,
                TenPhong = dto.TenPhong,
                MaLoaiPhong = dto.MaLoaiPhong,
                SoTang = dto.SoTang,
                TrangThai = dto.TrangThai,
                TrangThaiDonDep = dto.TrangThaiDonDep,
                IsCheckInToday = dto.IsCheckInToday,
                IsCheckOutToday = dto.IsCheckOutToday,
                LoaiPhong = new LoaiPhong
                {
                    MaLoaiPhong = dto.MaLoaiPhong,
                    TenLoaiPhong = dto.TenLoaiPhong,
                    GiaMacDinh = dto.GiaMacDinh
                }
            };
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

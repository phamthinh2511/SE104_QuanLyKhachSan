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
    public class PhongViewModel : INotifyPropertyChanged
    {
        // ── Data ──────────────────────────────────────────
        private ObservableCollection<PhongModel> _allPhongs;
        private ObservableCollection<PhongModel> _listPhong;

        public ObservableCollection<PhongModel> ListPhong
        {
            get => _listPhong;
            set { _listPhong = value; OnPropertyChanged(); }
        }
        // ── Filter combos ─────────────────────────────────
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

        // ── Thống kê ──────────────────────────────────────
        // Rental status (TrangThai)
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThai == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThai == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThai == 2) ?? 0;
        public int CountQuaHan => _allPhongs?.Count(p => p.TrangThai == 3) ?? 0;
        // Cleaning status (TrangThaiDonDep)
        public int CountCanDonDep => _allPhongs?.Count(p => p.TrangThaiDonDep == 1) ?? 0;
        public int CountBaoTri => _allPhongs?.Count(p => p.TrangThaiDonDep == 2) ?? 0;

        // ── Search ────────────────────────────────────────
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ExecuteSearch(); }
        }
        // Filter token format:
        //   "All"     = show everything
        //   "thue:N"  = filter by TrangThaiThue == N
        //   "don:N"   = filter by TrangThaiDonDep == N
        private string _currentStatusFilter = "All";

        // ── Commands ──────────────────────────────────────
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

        public PhongViewModel()
        {
            LoadData();

            // Lọc theo trạng thái
            FilterCommand = new RelayCommand<string>(filter =>
            {
                _currentStatusFilter = filter ?? "All";
                ApplyFilter();
            });

            // Check-in (Khách lẻ hoặc khách đã đặt)
            CheckInCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                string loai = p.TrangThai == 1 ? "khách đã đặt" : "khách lẻ";
                MessageBox.Show($"Bắt đầu check-in {loai} cho phòng {p.TenPhong}");
                // TODO: Mở dialog CheckInDialog
            });

            // Check-out
            CheckOutCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                MessageBox.Show($"Xử lý check-out phòng {p.TenPhong}");
                // TODO: Mở dialog CheckOutDialog
            });

            // Thêm dịch vụ
            AddServiceCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                var win = new QuanLyKhachSan_SE104.View.DichVu.DichVuPage();
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            // Đổi phòng
            DoiPhongCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                MessageBox.Show($"Đổi phòng từ {p.TenPhong}");
            });

            // Đổi trạng thái dọn dẹp
            DoiTrangThaiDonDepCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;

                try
                {
                    using var ctx = new QuanLyKhachSanContext();
                    var phongDb = ctx.Phongs.Find(p.MaPhong);
                    if (phongDb == null) return;

                    // Logic xoay vòng dựa trên bảng trạng thái của bạn:
                    // 0 (Sạch) -> 1 (Đang dọn)
                    // 1 (Đang dọn) -> 0 (Sạch)
                    // 2 (Bảo trì) -> 0 (Sạch) - Manual reset
                    int trangThaiCu = phongDb.TrangThaiDonDep;
                    int trangThaiMoi = trangThaiCu switch
                    {
                        0 => 1, // Nếu sạch thì chuyển sang Đang dọn
                        1 => 0, // Nếu đang dọn xong thì chuyển sang Sạch
                        2 => 0, // Nếu bảo trì xong thì chuyển sang Sạch
                        _ => 0
                    };

                    phongDb.TrangThaiDonDep = trangThaiMoi;
                    ctx.SaveChanges();

                    // Cập nhật text để thông báo
                    string label = trangThaiMoi switch
                    {
                        0 => "SẠCH",
                        1 => "ĐANG DỌN",
                        2 => "BẢO TRÌ",
                        _ => "SẠCH"
                    };

                    // Làm mới dữ liệu hiển thị trên danh sách
                    LoadData();

                    MessageBox.Show($"Phòng {p.TenPhong} đã chuyển sang trạng thái: {label}", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi cập nhật: " + ex.Message);
                }
            });

            // Hủy đặt phòng
            HuyDatPhongCommand = new RelayCommand<PhongModel>(p =>
            {
                if (p == null) return;
                var result = MessageBox.Show(
                    $"Xác nhận hủy đặt phòng {p.TenPhong}?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show($"Đã hủy đặt phòng {p.TenPhong}");
                }
            });

            // Sửa phòng
            EditRoomCommand = new RelayCommand<PhongModel>((p) => {
                if (p != null)
                {
                    var editDialog = new PhongModal(p);
                    if (editDialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            });

            // Thêm phòng mới
            AddRoomCommand = new RelayCommand<object>((p) => {
                var addDialog = new PhongModal(null);
                if (addDialog.ShowDialog() == true)
                {
                    LoadData();
                }
            });

            // Mở chi tiết phòng (Lịch sử đặt/Thông tin khách hiện tại)
            MoChiTietPhongCommand = new RelayCommand<PhongModel>(phong =>
            {
                if (phong == null) return;
                ChiTietDatPhongModel chiTiet = null;

                if (phong.TrangThai == 1 || phong.TrangThai == 2 || phong.TrangThai == 3)
                {
                    using var ctx = new QuanLyKhachSanContext();
                    // Phòng quá hạn (Phong.TrangThai=3) vẫn có booking đang active
                    // với TrangThaiDat = 1 (Đã đặt) hoặc 2 (Đang ở).
                    // KHÔNG filter TrangThaiDat=3 vì 3 = "Đã trả phòng" — booking đó đã xong.
                    chiTiet = ctx.ChiTietDatPhongs
                            .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                            .Include(c => c.ChiTietDichVus).ThenInclude(dv => dv.DichVu)
                            .FirstOrDefault(c => c.MaPhong == phong.MaPhong
                                && (c.DatPhong.TrangThaiDat == 1    // Đã đặt
                                || c.DatPhong.TrangThaiDat == 2));  // Đang ở (kể cả quá hạn)
                }

                var win = new QuanLyKhachSan_SE104.View.Phong.ChiTietPhong(phong, chiTiet);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
                LoadData();
            });
        }

        // ── Helper Methods ────────────────────────────────
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
                    // "All" or unknown: no status filter
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

            var lowerSearch = SearchText.Trim().ToLower();
            ListPhong = new ObservableCollection<PhongModel>(
                _allPhongs.Where(p => p.TenPhong?.ToLower().Contains(lowerSearch) == true)
            );
        }

        /// <summary>
        /// Call this whenever the Phong tab becomes visible (e.g. tab-switch, after booking, after check-in).
        /// Already called internally after MoChiTietPhongCommand closes.
        /// </summary>
        public void Refresh() => LoadData();

        public void LoadData()
        {
            using var ctx = new QuanLyKhachSanContext();
            // Auto-mark overdue rooms 
            var today = DateTime.Now.Date;
            var overdueChiTiets = ctx.ChiTietDatPhongs
                .Include(ct => ct.DatPhong)
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.NgayCheckOut < today &&           // strictly before today — not same-day midnight
                    (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                    ct.Phong.TrangThai != 3)
                .ToList();

            foreach (var ct in overdueChiTiets)
            {
                ct.Phong.TrangThai = 3; // Quá hạn
            }

            if (overdueChiTiets.Any())
                ctx.SaveChanges();

            // 1. Lấy toàn bộ phòng và SẮP XẾP THEO TÊN NGAY TỪ ĐẦU
            var allPhongsFromDb = ctx.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.TenPhong) // Sắp xếp tăng dần theo tên
                .ToList();
            _allPhongs = new ObservableCollection<PhongModel>(allPhongsFromDb);

            // 2. Xử lý ListTang (Thêm -1 làm giá trị "Tất cả")
            var tangs = _allPhongs.Select(p => p.SoTang).Distinct().OrderBy(t => t).ToList();
            ListTang = new ObservableCollection<int>();
            ListTang.Add(-1);
            foreach (var t in tangs) ListTang.Add(t);

            // 3. Xử lý ListLoaiPhong (Thêm một item "Tất cả")
            var loaiPhongs = ctx.LoaiPhongs.Where(lp => lp.IsDeleted == false).ToList();
            ListLoaiPhong = new ObservableCollection<LoaiPhong>();
            ListLoaiPhong.Add(new LoaiPhong { MaLoaiPhong = -1, TenLoaiPhong = "Tất cả" });
            foreach (var lp in loaiPhongs) ListLoaiPhong.Add(lp);

            ListPhong = new ObservableCollection<PhongModel>(_allPhongs);

            ApplyFilter();
            // Cập nhật UI cho các Badge/Button thống kê
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

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
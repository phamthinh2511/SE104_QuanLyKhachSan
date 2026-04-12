using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM
{
    public class PhongViewModel : INotifyPropertyChanged
    {
        // ── Data ──────────────────────────────────────────
        private ObservableCollection<Phong> _allPhongs;
        private ObservableCollection<Phong> _listPhong;

        public ObservableCollection<Phong> ListPhong
        {
            get => _listPhong;
            set { _listPhong = value; OnPropertyChanged(); }
        }

        // ── Thống kê — dùng TrangThai nhất quán ──────────
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThai == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThai == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThai == 2) ?? 0;
        public int CountQuaHan => _allPhongs?.Count(p => p.TrangThai == 3) ?? 0;
        public int CountCanDonDep => _allPhongs?.Count(p => p.TrangThai == 4) ?? 0;
        public int CountBaoTri => _allPhongs?.Count(p => p.TrangThai == 5) ?? 0; // FIX: tách riêng

        // ── Search ────────────────────────────────────────
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ExecuteSearch(); }
        }

        // ── Commands ──────────────────────────────────────
        public ICommand FilterCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand AddServiceCommand { get; }
        public ICommand DoiPhongCommand { get; }
        public ICommand DoiTrangThaiDonDepCommand { get; }
        public ICommand HuyDatPhongCommand { get; }

        public PhongViewModel()
        {
            LoadData();

            // Lọc theo trạng thái
            FilterCommand = new RelayCommand<string>(p =>
            {
                ApplyFilter(p);
            });

            // Check-in — dùng cho cả phòng trống (khách lẻ) và phòng đã đặt
            CheckInCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                string loai = p.TrangThai == 1 ? "khách đã đặt" : "khách lẻ";
                System.Windows.MessageBox.Show($"Bắt đầu check-in {loai} cho phòng {p.TenPhong}");
                // TODO: Mở dialog CheckInDialog, truyền p vào
            });

            // Check-out — chỉ dùng khi đang ở (TrangThai == 2)
            CheckOutCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                System.Windows.MessageBox.Show($"Xử lý check-out phòng {p.TenPhong}");
                // TODO: Mở dialog CheckOutDialog
            });

            // Thêm dịch vụ — chỉ khi đang ở
            AddServiceCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                var win = new QuanLyKhachSan_SE104.View.DichVu.DichVuPage();
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            // Đổi phòng — chỉ khi đang ở
            DoiPhongCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                System.Windows.MessageBox.Show($"Đổi phòng từ {p.TenPhong}");
                // TODO: Mở dialog chọn phòng mới
            });

            // Đổi trạng thái dọn dẹp — dùng cho phòng trống
            DoiTrangThaiDonDepCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                System.Windows.MessageBox.Show($"Đổi trạng thái dọn dẹp phòng {p.TenPhong}");
                // TODO: Toggle TrangThaiDonDep và save DB
            });

            // Hủy đặt — chỉ khi đã đặt (TrangThai == 1)
            HuyDatPhongCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                var result = System.Windows.MessageBox.Show(
                    $"Xác nhận hủy đặt phòng {p.TenPhong}?",
                    "Xác nhận",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // TODO: Gọi service hủy đặt
                    System.Windows.MessageBox.Show($"Đã hủy đặt phòng {p.TenPhong}");
                }
            });
        }

        private void ApplyFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "All")
                ListPhong = new ObservableCollection<Phong>(_allPhongs);
            else if (int.TryParse(filter, out int status))
                ListPhong = new ObservableCollection<Phong>(_allPhongs.Where(x => x.TrangThai == status));
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListPhong = new ObservableCollection<Phong>(_allPhongs);
                return;
            }

            var lowerSearch = SearchText.Trim().ToLower();
            ListPhong = new ObservableCollection<Phong>(
                // FIX: Thêm null check cho TenPhong tránh NullReferenceException
                _allPhongs.Where(p => p.TenPhong?.ToLower().Contains(lowerSearch) == true)
            );
        }

        private void LoadData()
        {
            _allPhongs = new ObservableCollection<Phong>
            {
                new Phong { TenPhong = "101", TrangThai = 0, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Standard",  GiaMacDinh = 300000  } },
                new Phong { TenPhong = "102", TrangThai = 2, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Deluxe",    GiaMacDinh = 500000  } },
                new Phong { TenPhong = "103", TrangThai = 1, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP",       GiaMacDinh = 1200000 } },
                new Phong { TenPhong = "104", TrangThai = 3, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP",       GiaMacDinh = 1200000 } },
                new Phong { TenPhong = "106", TrangThai = 4, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP",       GiaMacDinh = 1200000 } },
                new Phong { TenPhong = "105", TrangThai = 5, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP",       GiaMacDinh = 1200000 } },
            };
            ListPhong = new ObservableCollection<Phong>(_allPhongs);
        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
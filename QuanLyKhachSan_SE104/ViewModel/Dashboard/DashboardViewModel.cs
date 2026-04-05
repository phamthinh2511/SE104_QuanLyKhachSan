using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyKhachSan_SE104.ViewModel.Dashboard
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ── Data ──────────────────────────────────────────
        private ObservableCollection<Phong> _allPhongs; // Danh sách gốc để lọc
        private ObservableCollection<Phong> _listPhong; // Danh sách hiển thị
        public ObservableCollection<Phong> ListPhong
        {
            get => _listPhong;
            set { _listPhong = value; OnPropertyChanged(); }
        }

        // ── Thống kê (Sẽ tự update khi List thay đổi) ─────
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThaiThue == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThaiThue == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThaiThue == 2) ?? 0;

        // ── Search & Filter Property ──────────────────────
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

        public DashboardViewModel()
        {
            LoadData();

            // 1. Lọc theo trạng thái (0, 1, 2 hoặc "All")
            FilterCommand = new RelayCommand<string>(p =>
            {
                if (p == "All")
                    ListPhong = new ObservableCollection<Phong>(_allPhongs);
                else
                {
                    int status = int.Parse(p);
                    ListPhong = new ObservableCollection<Phong>(_allPhongs.Where(x => x.TrangThaiThue == status));
                }
            });

            // 2. Xử lý Check-in: Có thể mở một Dialog hoặc chuyển trang qua MainViewModel
            CheckInCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                // Logic: Thông báo hoặc điều hướng
                System.Windows.MessageBox.Show($"Bắt đầu nhận phòng cho {p.TenPhong}");
            });

            // 3. Xử lý Trả phòng
            CheckOutCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                System.Windows.MessageBox.Show($"Xử lý trả phòng {p.TenPhong}");
            });

            // 4. Thêm dịch vụ
            AddServiceCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                System.Windows.MessageBox.Show($"Thêm dịch vụ cho phòng {p.TenPhong}");
            });
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                ListPhong = new ObservableCollection<Phong>(_allPhongs);
            else
            {
                var lowerSearch = SearchText.ToLower();
                ListPhong = new ObservableCollection<Phong>(
                    _allPhongs.Where(p => p.TenPhong.ToLower().Contains(lowerSearch))
                );
            }
        }

        private void LoadData()
        {
            _allPhongs = new ObservableCollection<Phong>
            {
                new Phong { TenPhong = "101", TrangThaiThue = 0, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Standard", GiaMacDinh = 300000 } },
                new Phong { TenPhong = "102", TrangThaiThue = 2, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Deluxe", GiaMacDinh = 500000 } },
                new Phong { TenPhong = "103", TrangThaiThue = 1, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP", GiaMacDinh = 1200000 } }
            };
            ListPhong = new ObservableCollection<Phong>(_allPhongs);
        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
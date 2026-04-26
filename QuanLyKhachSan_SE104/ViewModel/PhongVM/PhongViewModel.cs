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

// Alias using theo yêu cầu
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

        // ── Thống kê ──────────────────────────────────────
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThai == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThai == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThai == 2) ?? 0;
        public int CountQuaHan => _allPhongs?.Count(p => p.TrangThai == 3) ?? 0;
        public int CountCanDonDep => _allPhongs?.Count(p => p.TrangThai == 4) ?? 0;
        public int CountBaoTri => _allPhongs?.Count(p => p.TrangThai == 5) ?? 0;

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
        public ICommand EditRoomCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand MoChiTietPhongCommand { get; }

        public PhongViewModel()
        {
            LoadData();

            // Lọc theo trạng thái
            FilterCommand = new RelayCommand<string>(p =>
            {
                ApplyFilter(p);
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
                MessageBox.Show($"Đổi trạng thái dọn dẹp phòng {p.TenPhong}");
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

                // Nếu phòng đang có khách hoặc đã đặt, lấy thông tin chi tiết
                if (phong.TrangThai == 1 || phong.TrangThai == 2)
                {
                    using (var context = new QuanLyKhachSanContext())
                    {
                        chiTiet = context.ChiTietDatPhongs
                            .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                            .Include(c => c.ChiTietDichVus).ThenInclude(dv => dv.DichVu)
                            .FirstOrDefault(c => c.MaPhong == phong.MaPhong
                                              && c.NgayCheckOut >= DateTime.Today);
                    }
                }

                // Giả sử đường dẫn View của bạn là QuanLyKhachSan_SE104.View.Phong.ChiTietPhong
                var win = new QuanLyKhachSan_SE104.View.Phong.ChiTietPhong(phong, chiTiet);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
                LoadData();
            });
        }

        // ── Helper Methods ────────────────────────────────
        private void ApplyFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "All")
                ListPhong = new ObservableCollection<PhongModel>(_allPhongs);
            else if (int.TryParse(filter, out int status))
                ListPhong = new ObservableCollection<PhongModel>(_allPhongs.Where(x => x.TrangThai == status));
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

        public void LoadData()
        {
            using (var context = new QuanLyKhachSanContext())
            {
                _allPhongs = new ObservableCollection<PhongModel>(
                    context.Phongs.Include(p => p.LoaiPhong).ToList()
                );
            }
            ListPhong = new ObservableCollection<PhongModel>(_allPhongs);

            // Cập nhật UI cho các Badge/Button thống kê
            OnPropertyChanged(nameof(CountTatCa));
            OnPropertyChanged(nameof(CountTrong));
            OnPropertyChanged(nameof(CountDaDat));
            OnPropertyChanged(nameof(CountDangO));
            OnPropertyChanged(nameof(CountQuaHan));
            OnPropertyChanged(nameof(CountCanDonDep));
            OnPropertyChanged(nameof(CountBaoTri));
        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
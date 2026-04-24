using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhong
{
    // ══════════════════════════════════════════════════════════════
    // VM for ChiTietDatPhongPage.xaml
    // Bindings used:
    //   SearchText, DatPhongCommand
    //   ListDatPhong  → MaDatPhong, KhachHang.HoTen, NgayDat, NhanVien.HoTen
    //   XemChiTietCommand, XoaCommand
    // ══════════════════════════════════════════════════════════════
    public class ChiTietDatPhongListViewModel : INotifyPropertyChanged
    {
        private readonly DatPhongDAL _dal = new DatPhongDAL();
        private ObservableCollection<DatPhongDTO> _allItems = new();

        // ── ListDatPhong ──────────────────────────────────────────
        private ObservableCollection<DatPhongDTO> _listDatPhong = new();
        public ObservableCollection<DatPhongDTO> ListDatPhong
        {
            get => _listDatPhong;
            set { _listDatPhong = value; OnPropertyChanged(); }
        }

        // ── SearchText ────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand DatPhongCommand { get; }
        public ICommand XemChiTietCommand { get; }
        public ICommand XoaCommand { get; }

        // Raised when the Page should navigate to DatPhongPage
        public event Action NavigateToDatPhong;
        // Raised when user clicks "Chi tiết" — opens ChiTietDatPhongWindow
        public event Action<DatPhongDTO> OpenChiTietWindow;

        public ChiTietDatPhongListViewModel()
        {
            DatPhongCommand = new RelayCommand(ExecuteDatPhong);
            XemChiTietCommand = new RelayCommand<DatPhongDTO>(ExecuteXemChiTiet);
            XoaCommand = new RelayCommand<DatPhongDTO>(ExecuteXoa);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var data = _dal.LayDanhSachDatPhong();
                _allItems = new ObservableCollection<DatPhongDTO>(data);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListDatPhong = new ObservableCollection<DatPhongDTO>(_allItems);
            }
            else
            {
                var keyword = SearchText.Trim().ToLower();
                var filtered = _allItems
                    .Where(x => x.TenKhachHang?.ToLower().Contains(keyword) == true);
                ListDatPhong = new ObservableCollection<DatPhongDTO>(filtered);
            }
        }

        private void ExecuteDatPhong() => NavigateToDatPhong?.Invoke();

        private void ExecuteXemChiTiet(DatPhongDTO item)
        {
            if (item == null) return;
            try
            {
                // Load detail rows from DB, then fire event so the View opens the Window
                item.DanhSachChiTiet = _dal.LayChiTietCacPhong(item.MaDatPhong);
                OpenChiTietWindow?.Invoke(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message);
            }
        }

        private void ExecuteXoa(DatPhongDTO item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show(
                $"Xóa phiếu #{item.MaDatPhong} của khách {item.TenKhachHang}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (_dal.XoaDatPhong(item.MaDatPhong))
                {
                    MessageBox.Show("Xóa thành công!");
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    // ══════════════════════════════════════════════════════════════
    // VM for ChiTietDatPhongWindow.xaml
    // Bindings used:
    //   TieuDe, TenKhachHang, NgayDat, TenNhanVien
    //   DanhSachChiTiet → TenPhong, NgayCheckIn, NgayCheckOut, SoNguoi
    //   ThoatCommand
    // ══════════════════════════════════════════════════════════════
    public class ChiTietDatPhongWindowViewModel : INotifyPropertyChanged
    {
        // Binding: TieuDe  (e.g. "PHIẾU ĐẶT PHÒNG #5")
        public string TieuDe { get; }

        // Binding: TenKhachHang
        public string TenKhachHang { get; }

        // Binding: NgayDat  (displayed as-is; format in XAML if needed)
        public string NgayDat { get; }

        // Binding: TenNhanVien
        public string TenNhanVien { get; }

        // Binding: DanhSachChiTiet  (ItemsControl)
        public ObservableCollection<ChiTietPhongDTO> DanhSachChiTiet { get; }

        // Binding: ThoatCommand
        public ICommand ThoatCommand { get; }

        // Raised so the View can close itself
        public event Action RequestClose;

        public ChiTietDatPhongWindowViewModel(DatPhongDTO dto)
        {
            TieuDe = dto.TieuDe;
            TenKhachHang = dto.TenKhachHang;
            NgayDat = dto.NgayDat.ToString("dd/MM/yyyy");
            TenNhanVien = dto.TenNhanVien;
            DanhSachChiTiet = new ObservableCollection<ChiTietPhongDTO>(dto.DanhSachChiTiet);

            ThoatCommand = new RelayCommand(() => RequestClose?.Invoke());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.ViewModel.DichVuVM
{
    // ── Item trong giỏ (bên phải) ──────────────────────
    public class DichVuDaChon : INotifyPropertyChanged
    {
        public DichVu DichVu { get; set; }

        private int _soLuong;
        public int SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThanhTien));
                OnPropertyChanged(nameof(ThanhTienText));
            }
        }

        public decimal ThanhTien => DichVu.DonGia * SoLuong;
        public string ThanhTienText => ThanhTien.ToString("#,0") + "₫";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ── Item trong danh sách bên trái ──────────────────
    public class DichVuItem : INotifyPropertyChanged
    {
        public DichVu DichVu { get; set; }

        private int _soLuong;
        public int SoLuong
        {
            get => _soLuong;
            set
            {
                if (value < 0) return;
                _soLuong = value;
                OnPropertyChanged();
            }
        }

        public string TenLoai => DichVu.LoaiDichVu switch
        {
            0 => "Đồ ăn",
            1 => "Đồ uống",
            2 => "Giải trí",
            3 => "Vận chuyển",
            _ => "Khác"
        };

        public string DonGiaText => DichVu.DonGia.ToString("#,0") + "₫";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ── ViewModel chính ────────────────────────────────
    public class DichVuViewModel : INotifyPropertyChanged
    {
        private readonly DichVuDAL _dal = new();

        // Danh sách gốc tất cả dịch vụ (wrapped)
        private List<DichVuItem> _allItems;

        // Danh sách hiển thị bên trái (theo filter)
        private ObservableCollection<DichVuItem> _danhSachDichVu;
        public ObservableCollection<DichVuItem> DanhSachDichVu
        {
            get => _danhSachDichVu;
            set { _danhSachDichVu = value; OnPropertyChanged(); }
        }

        // Giỏ dịch vụ đã chọn bên phải
        public ObservableCollection<DichVuDaChon> DanhSachDaChon { get; set; } = new();

        // Tổng tiền
        public decimal TongTien => DanhSachDaChon.Sum(x => x.ThanhTien);
        public string TongTienText => TongTien.ToString("#,0") + "₫";

        // Combobox nhóm
        public List<string> DanhSachNhom { get; } = new()
        {
            "Tất cả", "Đồ ăn", "Đồ uống", "Giải trí", "Vận chuyển"
        };

        private string _nhomDuocChon = "Tất cả";
        public string NhomDuocChon
        {
            get => _nhomDuocChon;
            set { _nhomDuocChon = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // Search
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // Commands
        public ICommand TangSoLuongCommand { get; }
        public ICommand GiamSoLuongCommand { get; }
        public ICommand XoaDichVuCommand { get; }
        public ICommand LuuCommand { get; }
        public ICommand ThoatCommand { get; }

        /// <summary>
        /// Set by ChiTietPhongViewModel BEFORE opening the window.
        /// Must be a valid MaChiTietDatPhong (> 0) for Lưu to write to DB.
        /// </summary>
        public int MaChiTietDatPhong { get; set; }

        /// <summary>
        /// Called after a successful save so ChiTietPhongViewModel can close the window.
        /// </summary>
        public Action CloseAction { get; set; }

        /// <summary>
        /// After a successful save, holds the newly-added rows so the caller can
        /// refresh its service list without an extra DB round-trip.
        /// </summary>
        public List<ChiTietDichVuDTO> SavedItems { get; private set; } = new();

        public DichVuViewModel()
        {
            LoadData();

            TangSoLuongCommand = new RelayCommand<DichVuItem>(item =>
            {
                if (item == null) return;
                item.SoLuong++;
                SyncToCart(item);
            });

            GiamSoLuongCommand = new RelayCommand<DichVuItem>(item =>
            {
                if (item == null || item.SoLuong <= 0) return;
                item.SoLuong--;
                SyncToCart(item);
            });

            XoaDichVuCommand = new RelayCommand<DichVuDaChon>(selected =>
            {
                if (selected == null) return;
                var left = _allItems.FirstOrDefault(x => x.DichVu.MaDichVu == selected.DichVu.MaDichVu);
                if (left != null) left.SoLuong = 0;
                DanhSachDaChon.Remove(selected);
                OnPropertyChanged(nameof(TongTienText));
                OnPropertyChanged(nameof(TongTien));
            });

            LuuCommand = new RelayCommand<object>(_ =>
            {
                if (DanhSachDaChon.Count == 0)
                {
                    MessageBox.Show("Chưa chọn dịch vụ nào.", "Thông báo");
                    return;
                }

                if (MaChiTietDatPhong <= 0)
                {
                    MessageBox.Show("Không xác định được phòng đang ở. Vui lòng thử lại.", "Lỗi");
                    return;
                }

                try
                {
                    var items = DanhSachDaChon
                        .Select(x => (x.DichVu.MaDichVu, x.SoLuong, x.DichVu.DonGia));

                    _dal.LuuChiTietDichVu(MaChiTietDatPhong, items);

                    // Build SavedItems so the caller can refresh without an extra query
                    SavedItems = DanhSachDaChon.Select(x => new ChiTietDichVuDTO
                    {
                        TenDichVu = x.DichVu.TenDichVu,
                        DonGia = x.DichVu.DonGia,
                        SoLuong = x.SoLuong
                    }).ToList();

                    MessageBox.Show($"Đã lưu {DanhSachDaChon.Count} dịch vụ. Tổng: {TongTienText}", "Thành công");
                    CloseAction?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu dịch vụ: " + ex.Message, "Lỗi");
                }
            });

            ThoatCommand = new RelayCommand<object>(_ => CloseAction?.Invoke());
        }

        private void SyncToCart(DichVuItem item)
        {
            var existing = DanhSachDaChon.FirstOrDefault(x => x.DichVu.MaDichVu == item.DichVu.MaDichVu);

            if (item.SoLuong > 0)
            {
                if (existing == null)
                    DanhSachDaChon.Add(new DichVuDaChon { DichVu = item.DichVu, SoLuong = item.SoLuong });
                else
                    existing.SoLuong = item.SoLuong;
            }
            else
            {
                if (existing != null) DanhSachDaChon.Remove(existing);
            }

            OnPropertyChanged(nameof(TongTienText));
            OnPropertyChanged(nameof(TongTien));
        }

        private void ApplyFilter()
        {
            var result = _allItems.AsEnumerable();

            if (NhomDuocChon != "Tất cả")
                result = result.Where(x => x.TenLoai == NhomDuocChon);

            if (!string.IsNullOrWhiteSpace(SearchText))
                result = result.Where(x => x.DichVu.TenDichVu?
                    .ToLower().Contains(SearchText.Trim().ToLower()) == true);

            DanhSachDichVu = new ObservableCollection<DichVuItem>(result);
        }

        private void LoadData()
        {
            try
            {
                var danhSach = _dal.LayDanhSachDichVu();
                _allItems = danhSach.Select(d => new DichVuItem { DichVu = d, SoLuong = 0 }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load dữ liệu dịch vụ: {ex.Message}", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                // Khởi tạo danh sách rỗng để tránh lỗi null ở các phần khác
                _allItems = new List<DichVuItem>();
            }

            DanhSachDichVu = new ObservableCollection<DichVuItem>(_allItems);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
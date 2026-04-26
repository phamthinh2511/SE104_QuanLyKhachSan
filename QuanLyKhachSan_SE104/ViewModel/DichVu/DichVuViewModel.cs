using QuanLyKhachSan_SE104.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Utilities;

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

        // Action để đóng Window từ bên ngoài
        public int MaChiTietDatPhong { get; set; }
        public Action CloseAction { get; set; }

        public DichVuViewModel()
        {
            LoadData();

            // Tăng số lượng item bên trái → tự động sync sang giỏ
            TangSoLuongCommand = new RelayCommand<DichVuItem>(item =>
            {
                if (item == null) return;
                item.SoLuong++;
                SyncToCart(item);
            });

            // Giảm số lượng
            GiamSoLuongCommand = new RelayCommand<DichVuItem>(item =>
            {
                if (item == null || item.SoLuong <= 0) return;
                item.SoLuong--;
                SyncToCart(item);
            });

            // Xóa khỏi giỏ (bên phải)
            XoaDichVuCommand = new RelayCommand<DichVuDaChon>(selected =>
            {
                if (selected == null) return;

                // Reset số lượng bên trái
                var left = _allItems.FirstOrDefault(x => x.DichVu.MaDichVu == selected.DichVu.MaDichVu);
                if (left != null) left.SoLuong = 0;

                DanhSachDaChon.Remove(selected);
                OnPropertyChanged(nameof(TongTienText));
                OnPropertyChanged(nameof(TongTien));
            });

            LuuCommand = new RelayCommand<object>(_ =>
            {
                // TODO: Gọi service lưu ChiTietDichVu vào DB
                System.Windows.MessageBox.Show(
                    $"Đã lưu {DanhSachDaChon.Count} dịch vụ. Tổng: {TongTienText}",
                    "Thành công");
                CloseAction?.Invoke();
            });

            ThoatCommand = new RelayCommand<object>(_ => CloseAction?.Invoke());
        }

        // Khi số lượng bên trái thay đổi → cập nhật giỏ bên phải
        private void SyncToCart(DichVuItem item)
        {
            var existing = DanhSachDaChon.FirstOrDefault(x => x.DichVu.MaDichVu == item.DichVu.MaDichVu);

            if (item.SoLuong > 0)
            {
                if (existing == null)
                {
                    DanhSachDaChon.Add(new DichVuDaChon
                    {
                        DichVu = item.DichVu,
                        SoLuong = item.SoLuong
                    });
                }
                else
                {
                    existing.SoLuong = item.SoLuong;
                }
            }
            else
            {
                if (existing != null)
                    DanhSachDaChon.Remove(existing);
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
            // TODO: Thay bằng load từ DB
            var danhSach = new List<DichVu>
            {
                new() { MaDichVu=1, LoaiDichVu=0, TenDichVu="Mì xào",       DonGia=25000 },
                new() { MaDichVu=2, LoaiDichVu=0, TenDichVu="Cơm chiên",     DonGia=30000 },
                new() { MaDichVu=3, LoaiDichVu=0, TenDichVu="Phở bò",        DonGia=45000 },
                new() { MaDichVu=4, LoaiDichVu=1, TenDichVu="Nước suối",     DonGia=10000 },
                new() { MaDichVu=5, LoaiDichVu=1, TenDichVu="Coca Cola",     DonGia=15000 },
                new() { MaDichVu=6, LoaiDichVu=1, TenDichVu="Bia Tiger",     DonGia=20000 },
                new() { MaDichVu=7, LoaiDichVu=2, TenDichVu="Karaoke 1h",    DonGia=150000},
                new() { MaDichVu=8, LoaiDichVu=2, TenDichVu="Massage 60p",   DonGia=200000},
                new() { MaDichVu=9, LoaiDichVu=3, TenDichVu="Taxi sân bay",  DonGia=300000},
            };

            _allItems = danhSach.Select(d => new DichVuItem { DichVu = d, SoLuong = 0 }).ToList();
            DanhSachDichVu = new ObservableCollection<DichVuItem>(_allItems);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
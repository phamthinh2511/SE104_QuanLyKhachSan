using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.Dashboard
{
    // ── Helper: dịch vụ đã chọn kèm số lượng ──
    public class SelectedDichVuItem : INotifyPropertyChanged
    {
        public DichVu DichVu { get; set; }

        private int _soLuong = 1;
        public int SoLuong
        {
            get => _soLuong;
            set { _soLuong = value; OnPropertyChanged(); OnPropertyChanged(nameof(ThanhTien)); }
        }

        public decimal ThanhTien => DichVu.DonGia * SoLuong;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        // ── Data ──────────────────────────────────────────
        private ObservableCollection<Phong> _allPhongs;
        private ObservableCollection<Phong> _listPhong;
        public ObservableCollection<Phong> ListPhong
        {
            get => _listPhong;
            set { _listPhong = value; OnPropertyChanged(); }
        }

        // ── Thống kê ─────────────────────────────────────
        public int CountTatCa => _allPhongs?.Count ?? 0;
        public int CountTrong => _allPhongs?.Count(p => p.TrangThaiThue == 0) ?? 0;
        public int CountDaDat => _allPhongs?.Count(p => p.TrangThaiThue == 1) ?? 0;
        public int CountDangO => _allPhongs?.Count(p => p.TrangThaiThue == 2) ?? 0;

        // ── Search ───────────────────────────────────────
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ExecuteSearch(); }
        }

        // ══════════════════════════════════════════════════
        // POPUP THAO TÁC NHANH
        // ══════════════════════════════════════════════════
        private Phong _selectedPhong;
        public Phong SelectedPhong
        {
            get => _selectedPhong;
            set { _selectedPhong = value; OnPropertyChanged(); }
        }

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set { _isPopupOpen = value; OnPropertyChanged(); }
        }

        // ══════════════════════════════════════════════════
        // POPUP THÊM DỊCH VỤ
        // ══════════════════════════════════════════════════
        private bool _isServicePopupOpen;
        public bool IsServicePopupOpen
        {
            get => _isServicePopupOpen;
            set { _isServicePopupOpen = value; OnPropertyChanged(); }
        }

        private ObservableCollection<DichVu> _availableDichVus;
        public ObservableCollection<DichVu> AvailableDichVus
        {
            get => _availableDichVus;
            set { _availableDichVus = value; OnPropertyChanged(); }
        }

        private ObservableCollection<SelectedDichVuItem> _selectedDichVus;
        public ObservableCollection<SelectedDichVuItem> SelectedDichVus
        {
            get => _selectedDichVus;
            set { _selectedDichVus = value; OnPropertyChanged(); OnPropertyChanged(nameof(TongTienDichVu)); }
        }

        public decimal TongTienDichVu => SelectedDichVus?.Sum(x => x.ThanhTien) ?? 0;

        // ══════════════════════════════════════════════════
        // POPUP HÓA ĐƠN (sau checkout)
        // ══════════════════════════════════════════════════
        private bool _isHoaDonOpen;
        public bool IsHoaDonOpen
        {
            get => _isHoaDonOpen;
            set { _isHoaDonOpen = value; OnPropertyChanged(); }
        }

        private string _hoaDonTenKhach;
        public string HoaDonTenKhach { get => _hoaDonTenKhach; set { _hoaDonTenKhach = value; OnPropertyChanged(); } }

        private string _hoaDonSoPhong;
        public string HoaDonSoPhong { get => _hoaDonSoPhong; set { _hoaDonSoPhong = value; OnPropertyChanged(); } }

        private string _hoaDonThoiGian;
        public string HoaDonThoiGian { get => _hoaDonThoiGian; set { _hoaDonThoiGian = value; OnPropertyChanged(); } }

        private decimal _hoaDonTienPhong;
        public decimal HoaDonTienPhong { get => _hoaDonTienPhong; set { _hoaDonTienPhong = value; OnPropertyChanged(); } }

        private decimal _hoaDonTienDichVu;
        public decimal HoaDonTienDichVu { get => _hoaDonTienDichVu; set { _hoaDonTienDichVu = value; OnPropertyChanged(); } }

        private decimal _hoaDonTongTien;
        public decimal HoaDonTongTien { get => _hoaDonTongTien; set { _hoaDonTongTien = value; OnPropertyChanged(); } }

        public ICommand CloseHoaDonCommand { get; private set; }

        // ══════════════════════════════════════════════════
        // COMMANDS
        // ══════════════════════════════════════════════════
        public ICommand FilterCommand { get; }
        public ICommand RoomClickCommand { get; }
        public ICommand ClosePopupCommand { get; }

        // Phòng trống (0)
        public ICommand CheckInKhachLeCommand { get; }
        public ICommand ToggleDonDepCommand { get; }

        // Phòng đang ở (2)
        public ICommand AddServiceCommand { get; }
        public ICommand ChangeRoomCommand { get; }
        public ICommand CheckOutCommand { get; }

        // Phòng đã đặt (1)
        public ICommand CheckInDaDatCommand { get; }

        // Popup dịch vụ
        public ICommand CloseServicePopupCommand { get; }
        public ICommand AddDichVuToListCommand { get; }
        public ICommand RemoveDichVuFromListCommand { get; }
        public ICommand SubmitDichVuCommand { get; }
        public ICommand TangSoLuongCommand { get; }
        public ICommand GiamSoLuongCommand { get; }

        public DashboardViewModel()
        {
            SelectedDichVus = new ObservableCollection<SelectedDichVuItem>();
            LoadData();
            LoadDichVu();

            // ── Lọc phòng ──
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

            // ── Click vào phòng → mở popup thao tác nhanh ──
            RoomClickCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                SelectedPhong = p;
                IsPopupOpen = true;
            });

            ClosePopupCommand = new RelayCommand(() => IsPopupOpen = false);

            // ══ PHÒNG TRỐNG (0) ══

            CheckInKhachLeCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                IsPopupOpen = false;
                var mainVM = Application.Current.MainWindow.DataContext as MainViewModel.MainViewModel;
                mainVM?.NavigateTo("Đặt phòng");
            });

            ToggleDonDepCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                p.TrangThaiDonDep = p.TrangThaiDonDep == 0 ? 2 : 0;
                IsPopupOpen = false;
                string trangThai = p.TrangThaiDonDep == 0 ? "Sạch" : "Cần dọn dẹp";
                MessageBox.Show($"Phòng {p.TenPhong} → {trangThai}", "Đổi trạng thái");
            });

            // ══ PHÒNG ĐANG Ở (2) ══

            // Thêm dịch vụ → mở popup dịch vụ (không navigate)
            AddServiceCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                IsPopupOpen = false;
                SelectedDichVus.Clear();
                OnPropertyChanged(nameof(TongTienDichVu));
                IsServicePopupOpen = true;
            });

            ChangeRoomCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                IsPopupOpen = false;
                MessageBox.Show($"Chức năng đổi phòng {p.TenPhong} đang phát triển.", "Đổi phòng");
            });

            // Check-out → xuất hóa đơn ngay
            CheckOutCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                IsPopupOpen = false;
                ExecuteCheckout(p);
            });

            CloseHoaDonCommand = new RelayCommand(() => IsHoaDonOpen = false);

            // ══ PHÒNG ĐÃ ĐẶT (1) ══

            CheckInDaDatCommand = new RelayCommand<Phong>(p =>
            {
                if (p == null) return;
                IsPopupOpen = false;
                var mainVM = Application.Current.MainWindow.DataContext as MainViewModel.MainViewModel;
                mainVM?.NavigateTo("Nhận phòng");
            });

            // ══ POPUP DỊCH VỤ ══

            CloseServicePopupCommand = new RelayCommand(() =>
            {
                IsServicePopupOpen = false;
                SelectedDichVus.Clear();
                OnPropertyChanged(nameof(TongTienDichVu));
            });

            AddDichVuToListCommand = new RelayCommand<DichVu>(dv =>
            {
                if (dv == null) return;
                var existing = SelectedDichVus.FirstOrDefault(x => x.DichVu.MaDichVu == dv.MaDichVu);
                if (existing != null)
                {
                    existing.SoLuong++;
                }
                else
                {
                    SelectedDichVus.Add(new SelectedDichVuItem { DichVu = dv, SoLuong = 1 });
                }
                OnPropertyChanged(nameof(TongTienDichVu));
            });

            RemoveDichVuFromListCommand = new RelayCommand<SelectedDichVuItem>(item =>
            {
                if (item == null) return;
                SelectedDichVus.Remove(item);
                OnPropertyChanged(nameof(TongTienDichVu));
            });

            TangSoLuongCommand = new RelayCommand<SelectedDichVuItem>(item =>
            {
                if (item == null) return;
                item.SoLuong++;
                OnPropertyChanged(nameof(TongTienDichVu));
            });

            GiamSoLuongCommand = new RelayCommand<SelectedDichVuItem>(item =>
            {
                if (item == null) return;
                if (item.SoLuong > 1)
                    item.SoLuong--;
                else
                    SelectedDichVus.Remove(item);
                OnPropertyChanged(nameof(TongTienDichVu));
            });

            SubmitDichVuCommand = new RelayCommand(() =>
            {
                if (SelectedDichVus.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 dịch vụ!", "Thông báo");
                    return;
                }

                string chiTiet = string.Join("\n", SelectedDichVus.Select(x =>
                    $"  - {x.DichVu.TenDichVu} x{x.SoLuong} = {x.ThanhTien:#,0}₫"));

                MessageBox.Show(
                    $"Đã gửi yêu cầu dịch vụ cho phòng {SelectedPhong.TenPhong}:\n{chiTiet}\n\nTổng: {TongTienDichVu:#,0}₫",
                    "Thành công");

                IsServicePopupOpen = false;
                SelectedDichVus.Clear();
                OnPropertyChanged(nameof(TongTienDichVu));
            });
        }

        private void ExecuteCheckout(Phong phong)
        {
            try
            {
                using (var db = new QuanLyKhachSanContext())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Tìm chi tiết đặt phòng đang ở của phòng này
                            var ctdp = db.ChiTietDatPhongs
                                .Include(c => c.DatPhong)
                                    .ThenInclude(d => d.KhachHang)
                                .Include(c => c.Phong)
                                .Include(c => c.ChiTietDichVus)
                                .FirstOrDefault(c => c.MaPhong == phong.MaPhong
                                    && c.DatPhong.TrangThaiDat == 2);

                            if (ctdp == null)
                            {
                                MessageBox.Show($"Không tìm thấy thông tin đặt phòng cho phòng {phong.TenPhong}!", "Lỗi");
                                return;
                            }

                            var datPhong = ctdp.DatPhong;

                            // Tính tiền phòng theo giờ
                            var soGio = (decimal)Math.Ceiling((DateTime.Now - ctdp.NgayCheckIn).TotalHours);
                            if (soGio < 1) soGio = 1;
                            decimal tongTienPhong = soGio * ctdp.GiaDat;

                            // Tính tiền dịch vụ
                            decimal tongTienDichVu = 0;
                            foreach (var dv in ctdp.ChiTietDichVus)
                                tongTienDichVu += dv.SoLuong * dv.DonGia;

                            decimal tongThanhToan = tongTienPhong + tongTienDichVu - datPhong.TienCoc;

                            // Tạo hóa đơn
                            var hoaDon = new HoaDon
                            {
                                MaDatPhong = datPhong.MaDatPhong,
                                MaNhanVien = 1,
                                TongTienPhong = tongTienPhong,
                                TongTienDichVu = tongTienDichVu,
                                PhuPhi = 0,
                                TienCoc = datPhong.TienCoc,
                                TongThanhToan = tongThanhToan,
                                NgayThanhToan = DateTime.Now,
                                PhuongThucThanhToan = 0,
                                TrangThaiThanhToan = "Đã thanh toán",
                                GhiChu = "Check-out"
                            };
                            db.HoaDons.Add(hoaDon);

                            // Cập nhật trạng thái
                            ctdp.NgayCheckOut = DateTime.Now;
                            var dbPhong = db.Phongs.Find(phong.MaPhong);
                            if (dbPhong != null)
                            {
                                dbPhong.TrangThaiThue = 0;
                                dbPhong.TrangThaiDonDep = 2;
                            }
                            datPhong.TrangThaiDat = 3;

                            db.SaveChanges();
                            transaction.Commit();

                            // Hiện popup hóa đơn
                            HoaDonTenKhach = datPhong.KhachHang?.HoTen ?? "Khách";
                            HoaDonSoPhong = phong.TenPhong;
                            HoaDonThoiGian = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                            HoaDonTienPhong = tongTienPhong;
                            HoaDonTienDichVu = tongTienDichVu;
                            HoaDonTongTien = tongThanhToan;
                            IsHoaDonOpen = true;

                            // Cập nhật trạng thái phòng trên UI
                            phong.TrangThaiThue = 0;
                            LoadData();
                            OnPropertyChanged(nameof(CountTatCa));
                            OnPropertyChanged(nameof(CountTrong));
                            OnPropertyChanged(nameof(CountDaDat));
                            OnPropertyChanged(nameof(CountDangO));
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Lỗi checkout: " + ex.Message, "Lỗi");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối DB: " + ex.Message, "Lỗi");
            }
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
                new Phong { TenPhong = "101", TrangThaiThue = 0, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Standard", GiaMacDinh = 50000 } },
                new Phong { TenPhong = "102", TrangThaiThue = 2, LoaiPhong = new LoaiPhong { TenLoaiPhong = "Deluxe", GiaMacDinh = 80000 } },
                new Phong { TenPhong = "103", TrangThaiThue = 1, LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP", GiaMacDinh = 150000 } }
            };
            ListPhong = new ObservableCollection<Phong>(_allPhongs);
        }

        private void LoadDichVu()
        {
            AvailableDichVus = new ObservableCollection<DichVu>
            {
                new DichVu { MaDichVu = 1, TenDichVu = "Nước suối", DonGia = 10000, LoaiDichVu = 1 },
                new DichVu { MaDichVu = 2, TenDichVu = "Coca Cola", DonGia = 15000, LoaiDichVu = 1 },
                new DichVu { MaDichVu = 3, TenDichVu = "Cà phê", DonGia = 25000, LoaiDichVu = 1 },
                new DichVu { MaDichVu = 4, TenDichVu = "Mì gói", DonGia = 20000, LoaiDichVu = 0 },
                new DichVu { MaDichVu = 5, TenDichVu = "Giặt ủi (1 bộ)", DonGia = 30000, LoaiDichVu = 2 },
                new DichVu { MaDichVu = 6, TenDichVu = "Khăn tắm thêm", DonGia = 15000, LoaiDichVu = 2 },
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

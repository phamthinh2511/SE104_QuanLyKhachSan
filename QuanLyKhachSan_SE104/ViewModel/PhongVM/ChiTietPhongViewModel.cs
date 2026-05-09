using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.DatPhong;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ChiTietDatPhongModel = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;
using DatPhongModel = QuanLyKhachSan_SE104.Model.DatPhong;
using PhongModel = QuanLyKhachSan_SE104.Model.Phong;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM
{
    public class ChiTietPhongViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly Window _window;

        private ObservableCollection<ChiTietDichVuDTO> _danhSachDichVu = new();
        public ObservableCollection<ChiTietDichVuDTO> DanhSachDichVu
        {
            get => _danhSachDichVu;
            set { _danhSachDichVu = value; OnPropertyChanged(); OnPropertyChanged(nameof(TongTienDichVuText)); }
        }

        public PhongModel Phong { get; }
        public ChiTietDatPhongModel ChiTietDatPhong { get; }

        public int SoDem => ChiTietDatPhong != null
            ? Math.Max(1, (ChiTietDatPhong.NgayCheckOut - ChiTietDatPhong.NgayCheckIn).Days)
            : 0;

        public string TongTienDichVuText
            => $"{DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0:#,0}₫";

        private readonly DichVuDAL _dichVuDal = new();

        public ICommand ThoatCommand => new RelayCommand(() => _window.Close());

        // Replace CheckInKhachLeCommand and DoiPhongCommand in ChiTietPhongViewModel.cs

        // ── Walk-in: blank customer form, room pre-selected, grid hidden ──────────────
        public ICommand CheckInKhachLeCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null) return;

            var vm = new DatPhongViewModel(phong);   // WalkIn constructor
            var page = new DatPhongPage { DataContext = vm };

            var win = new Window
            {
                Title = $"Check-in khách lẻ — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };

            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true)
                _window.Close();   // also close ChiTietPhong after successful check-in
        });

        // ── Đổi phòng: customer pre-filled, pick a different room ────────────────────
        public ICommand DoiPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            var vm = new DatPhongViewModel(phong, ChiTietDatPhong);  // DoiPhong constructor
            var page = new DatPhongPage { DataContext = vm };

            var win = new Window
            {
                Title = $"Đổi phòng — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };

            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true)
                _window.Close();   // close ChiTietPhong so parent reloads
        });


        // Cycle: 0 (Clean) → 2 (Needs Cleaning) → 1 (Cleaning In Progress) → 0
        public ICommand DoiTrangThaiDonDepCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();
                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p == null) return;

                p.TrangThaiDonDep = p.TrangThaiDonDep switch
                {
                    0 => 2,   // Sạch → Cần dọn
                    2 => 1,   // Cần dọn → Đang dọn
                    1 => 0,   // Đang dọn → Sạch
                    3 => 0,   // Bảo trì → Sạch (manual reset)
                    _ => 0
                };

                ctx.SaveChanges();
                var label = p.TrangThaiDonDep switch { 0 => "Sạch", 1 => "Đang dọn", 2 => "Cần dọn", 3 => "Bảo trì", _ => "" };
                MessageBox.Show($"Trạng thái dọn dẹp phòng {phong.TenPhong}: {label}", "Thông báo");
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        });

        public ICommand CheckInDaDatCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();

                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p != null) p.TrangThai = 2;

                var dat = ctx.DatPhongs.Find(ChiTietDatPhong.MaDatPhong);
                if (dat != null) dat.TrangThaiDat = 2;

                var ct = ctx.ChiTietDatPhongs.Find(ChiTietDatPhong.MaChiTietDatPhong);
                if (ct != null) ct.NgayCheckIn = DateTime.Now;

                ctx.SaveChanges();
                MessageBox.Show($"Check-in thành công phòng {phong.TenPhong}!", "Thông báo");
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi check-in: " + ex.Message); }
        });

        public ICommand HuyDatPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;
            if (MessageBox.Show($"Xác nhận hủy đặt phòng {phong.TenPhong}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();

                var dat = ctx.DatPhongs.Find(ChiTietDatPhong.MaDatPhong);
                if (dat != null) dat.TrangThaiDat = 4;

                var ct = ctx.ChiTietDatPhongs.Find(ChiTietDatPhong.MaChiTietDatPhong);
                if (ct != null) ctx.ChiTietDatPhongs.Remove(ct);

                var p = ctx.Phongs.Find(phong.MaPhong);
                if (p != null) p.TrangThai = 0;

                ctx.SaveChanges();
                MessageBox.Show($"Đã hủy đặt phòng {phong.TenPhong}.", "Thông báo");
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi hủy đặt: " + ex.Message); }
        });

        public ICommand ThemDichVuCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (ChiTietDatPhong == null)
            {
                MessageBox.Show("Không có thông tin phòng đang ở để thêm dịch vụ.", "Thông báo");
                return;
            }

            // Khởi tạo ViewModel cho cửa sổ Dịch vụ và truyền ID phòng vào TRƯỚC khi mở
            var vm = new DichVuVM.DichVuViewModel
            {
                MaChiTietDatPhong = ChiTietDatPhong.MaChiTietDatPhong
            };

            var win = new View.DichVu.DichVuPage { DataContext = vm };
            win.Owner = _window; // _window là biến lưu Window hiện tại

            // Thiết lập hành động đóng cửa sổ
            vm.CloseAction = () =>
            {
                win.DialogResult = true;
                win.Close();
            };

            // Sau khi cửa sổ đóng, nếu lưu thành công thì cập nhật ngay lên giao diện
            if (win.ShowDialog() == true && vm.SavedItems.Count > 0)
            {
                foreach (var item in vm.SavedItems)
                    DanhSachDichVu.Add(item); // Thêm món mới vào danh sách đang hiển thị

                // Báo cho UI biết là tổng tiền đã thay đổi để cập nhật con số mới
                OnPropertyChanged(nameof(TongTienDichVuText));
            }
        });


        public void ExecuteDoiPhong(PhongModel phongMoi)
        {
            if (ChiTietDatPhong == null || phongMoi == null) return;
            try
            {
                using var ctx = new QuanLyKhachSanContext();

                // Free old room
                var cu = ctx.Phongs.Find(Phong.MaPhong);
                if (cu != null) cu.TrangThai = 0;

                // Occupy new room
                var moi = ctx.Phongs.Find(phongMoi.MaPhong);
                if (moi != null) moi.TrangThai = 2;

                // Point ChiTietDatPhong to new room — services stay linked via MaChiTietDatPhong
                var ct = ctx.ChiTietDatPhongs.Find(ChiTietDatPhong.MaChiTietDatPhong);
                if (ct != null) ct.MaPhong = phongMoi.MaPhong;

                ctx.SaveChanges();
                MessageBox.Show($"Đã đổi sang phòng {phongMoi.TenPhong}.", "Thông báo");
                _window.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi đổi phòng: " + ex.Message); }
        }

        public ICommand CheckOutCommand => new RelayCommand<PhongModel>(phong =>
        {
            var vm = new HoaDonVM.HoaDonViewModel(
              ChiTietDatPhong.MaDatPhong,
              ChiTietDatPhong.MaChiTietDatPhong,
              1);          // pass your logged-in staff ID

            var page = new View.HoaDon.HoaDonPage { DataContext = vm };
            var win = new Window
            {
                Title = $"Hóa đơn — Phòng {Phong.TenPhong}",
                Width = 980,
                Height = 760,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };
            win.ShowDialog();
            _window.Close();   // refresh parent after payment
        });

        public ChiTietPhongViewModel(PhongModel phong, ChiTietDatPhongModel chiTietDatPhong, Window window)
        {
            Phong = phong;
            ChiTietDatPhong = chiTietDatPhong;
            _window = window;
            RefreshDichVu();
        }

        private void RefreshDichVu()
        {
            if (ChiTietDatPhong == null)
            {
                DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>();
                return;
            }

            // Gọi DAL để lấy danh sách dịch vụ thực tế từ DB
            var rows = _dichVuDal.LayDichVuTheoChiTiet(ChiTietDatPhong.MaChiTietDatPhong);

            DanhSachDichVu = new ObservableCollection<ChiTietDichVuDTO>(
                rows.Where(x => x.DichVu != null)
                    .Select(x => new ChiTietDichVuDTO
                    {
                        TenDichVu = x.DichVu.TenDichVu,
                        DonGia = x.DonGia,
                        SoLuong = x.SoLuong
                    }));
        }
    }
}
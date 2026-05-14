using Microsoft.EntityFrameworkCore;
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

        public int SoDem
        {
            get
            {
                if (ChiTietDatPhong == null) return 0;

                // Nếu khách đang ở (đã check-in), tính từ lúc check-in đến hiện tại
                // Nếu khách chưa check-in (mới đặt), tính theo dự kiến trong hợp đồng
                DateTime checkIn = ChiTietDatPhong.NgayCheckIn;
                DateTime checkOut = ChiTietDatPhong.NgayCheckOut;

                // Tính toán: Hiệu số ngày, làm tròn lên (Ceiling) và tối thiểu là 1 đêm
                double totalDays = (checkOut - checkIn).TotalDays;
                return Math.Max(1, (int)Math.Ceiling(totalDays));
            }
        }

        public string TongTienDichVuText
            => $"{DanhSachDichVu?.Sum(x => x.ThanhTien) ?? 0:#,0}₫";

        private readonly DichVuDAL _dichVuDal = new();

        public ICommand ThoatCommand => new RelayCommand(() => _window.Close());

        // ── Walk-in: blank customer form, room pre-selected, grid hidden ──────
        public ICommand CheckInKhachLeCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null) return;

            var vm = new DatPhongViewModel(phong);
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
                _window.Close();
        });

        // ── Đổi phòng (chỉ TrangThai = 2) ───────────────────────────────────
        public ICommand DoiPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            var vm = new DatPhongViewModel(phong, ChiTietDatPhong);
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
                _window.Close();
        });

        // ── Gia hạn phòng (chỉ TrangThai = 3 — quá hạn) ─────────────────────
        public ICommand GiaHanPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            if (phong == null || ChiTietDatPhong == null) return;

            // Cần load đầy đủ navigation property KhachHang cho DatPhong
            ChiTietDatPhongModel chiTietFull;
            using (var ctx = new QuanLyKhachSanContext())
            {
                chiTietFull = ctx.ChiTietDatPhongs
                    .Include(c => c.DatPhong).ThenInclude(d => d.KhachHang)
                    .FirstOrDefault(c => c.MaChiTietDatPhong == ChiTietDatPhong.MaChiTietDatPhong)
                    ?? ChiTietDatPhong;
            }

            var vm = new DatPhongViewModel(phong, chiTietFull, giaHan: true);
            var page = new DatPhongPage { DataContext = vm };

            var win = new Window
            {
                Title = $"Gia hạn phòng — Phòng {phong.TenPhong}",
                Width = 1100,
                Height = 700,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = _window
            };

            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };
            if (win.ShowDialog() == true)
                _window.Close();
        });

        // ── Đổi trạng thái dọn dẹp ───────────────────────────────────────────
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
                    0 => 2,
                    2 => 1,
                    1 => 0,
                    3 => 0,
                    _ => 0
                };

                ctx.SaveChanges();
                var label = p.TrangThaiDonDep switch
                {
                    0 => "Sạch",
                    1 => "Đang dọn",
                    2 => "Cần dọn",
                    3 => "Bảo trì",
                    _ => ""
                };
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

            var vm = new DichVuVM.DichVuViewModel
            {
                MaChiTietDatPhong = ChiTietDatPhong.MaChiTietDatPhong
            };

            var win = new View.DichVu.DichVuPage { DataContext = vm };
            win.Owner = _window;

            vm.CloseAction = () => { win.DialogResult = true; win.Close(); };

            if (win.ShowDialog() == true && vm.SavedItems.Count > 0)
            {
                foreach (var item in vm.SavedItems)
                    DanhSachDichVu.Add(item);

                OnPropertyChanged(nameof(TongTienDichVuText));
            }
        });

        public ICommand CheckOutCommand => new RelayCommand<PhongModel>(phong =>
        {
            var vm = new HoaDonVM.HoaDonViewModel(
                ChiTietDatPhong.MaDatPhong,
                ChiTietDatPhong.MaChiTietDatPhong,
                1); // TODO: logged-in staff ID

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
            _window.Close();
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
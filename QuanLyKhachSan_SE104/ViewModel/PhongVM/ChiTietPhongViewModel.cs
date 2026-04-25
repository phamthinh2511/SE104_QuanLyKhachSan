using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Utilities;
using PhongModel = QuanLyKhachSan_SE104.Model.Phong;
using ChiTietDatPhongModel = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM
{
    public class ChiTietPhongViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly Window _window;

        public PhongModel Phong { get; }
        public ChiTietDatPhongModel ChiTietDatPhong { get; }

        public int SoDem => ChiTietDatPhong != null
            ? Math.Max(1, (ChiTietDatPhong.NgayCheckOut - ChiTietDatPhong.NgayCheckIn).Days)
            : 0;

        public string TongTienDichVuText
        {
            get
            {
                if (ChiTietDatPhong?.ChiTietDichVus == null) return "0₫";
                var tong = ChiTietDatPhong.ChiTietDichVus.Sum(ct => ct.DonGia * ct.SoLuong);
                return $"{tong:#,0}₫";
            }
        }

        public ICommand ThoatCommand => new RelayCommand(() => _window.Close());

        public ICommand CheckInKhachLeCommand => new RelayCommand<PhongModel>(phong =>
        {
            // TODO: new NhanPhongWindow(phong).ShowDialog();
            MessageBox.Show($"Check-in khách lẻ: {phong.TenPhong}");
            _window.Close();
        });

        public ICommand DoiTrangThaiDonDepCommand => new RelayCommand<PhongModel>(phong =>
        {
            // TODO: gọi service cập nhật trạng thái
            MessageBox.Show($"Đổi trạng thái dọn dẹp: {phong.TenPhong}");
            _window.Close();
        });

        public ICommand CheckInDaDatCommand => new RelayCommand<PhongModel>(phong =>
        {
            // TODO: cập nhật TrangThai = 2
            MessageBox.Show($"Check-in khách đã đặt: {phong.TenPhong}");
            _window.Close();
        });

        public ICommand HuyDatPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            var result = MessageBox.Show(
                $"Bạn có chắc muốn hủy đặt phòng {phong.TenPhong}?",
                "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // TODO: cập nhật TrangThai = 0
                _window.Close();
            }
        });

        public ICommand ThemDichVuCommand => new RelayCommand<PhongModel>(phong =>
        {
            var win = new View.DichVu.DichVuPage();
            if (win.DataContext is DichVuVM.DichVuViewModel vm)
                vm.MaChiTietDatPhong = ChiTietDatPhong?.MaChiTietDatPhong ?? 0;
            win.Owner = _window;
            win.ShowDialog();
            OnPropertyChanged(nameof(TongTienDichVuText));
        });

        public ICommand DoiPhongCommand => new RelayCommand<PhongModel>(phong =>
        {
            // TODO: new DoiPhongWindow(phong, ChiTietDatPhong).ShowDialog();
            MessageBox.Show($"Đổi phòng: {phong.TenPhong}");
            _window.Close();
        });

        public ICommand CheckOutCommand => new RelayCommand<PhongModel>(phong =>
        {
            // TODO: new HoaDonWindow(ChiTietDatPhong).ShowDialog();
            MessageBox.Show($"Check-out: {phong.TenPhong}");
            _window.Close();
        });

        public ChiTietPhongViewModel(PhongModel phong, ChiTietDatPhongModel chiTietDatPhong, Window window)
        {
            Phong = phong;
            ChiTietDatPhong = chiTietDatPhong;
            _window = window;
        }
    }
}
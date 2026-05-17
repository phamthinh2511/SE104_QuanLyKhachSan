using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View;

///// hiển thị Pop-up danh sách phòng, chứa lệnh click vào phòng để mở Hóa Đơn
namespace QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM
{
    public class ChiTietDatPhongWindowViewModel : INotifyPropertyChanged
    {
        public string TieuDe { get; }
        public string TenKhachHang { get; }
        public string NgayDat { get; }
        public string TenNhanVien { get; }

        public ObservableCollection<ChiTietPhongDTO> DanhSachChiTiet { get; }

        public ICommand ThoatCommand { get; }
        public ICommand XemHoaDonCommand { get; }

        public event Action RequestClose;

        public ChiTietDatPhongWindowViewModel(DatPhongDTO dto)
        {
            TieuDe = dto.TieuDe;
            TenKhachHang = dto.TenKhachHang;
            NgayDat = dto.NgayDat.ToString("dd/MM/yyyy");
            TenNhanVien = dto.TenNhanVien;

            DanhSachChiTiet = new ObservableCollection<ChiTietPhongDTO>(dto.DanhSachChiTiet);

            XemHoaDonCommand = new RelayCommand<ChiTietPhongDTO>(ExecuteXemHoaDon);
            ThoatCommand = new RelayCommand(() => RequestClose?.Invoke());
        }

        private void ExecuteXemHoaDon(ChiTietPhongDTO roomInfo)
        {
            if (roomInfo == null) return;

            try
            {
                // Khởi tạo ViewModel và Window Hóa đơn chi tiết
                var vm = new HoaDonChiTietViewModel(roomInfo.MaChiTietDatPhong);
                var window = new HoaDonChiTietWindow(vm);

                // Mở cửa sổ Hóa Đơn đè lên cửa sổ Pop-up hiện tại
                window.Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở hóa đơn: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
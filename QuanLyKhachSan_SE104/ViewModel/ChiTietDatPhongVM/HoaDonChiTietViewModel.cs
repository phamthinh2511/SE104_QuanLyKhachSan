using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

/////// Hiển thị chi tiết hóa đơn khi click vào phòng trong Pop-up chi tiết đặt phòng
namespace QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM
{
    public class HoaDonChiTietViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Action CloseAction { get; set; }

        private HoaDonChiTietDTO _hoaDon;
        public HoaDonChiTietDTO HoaDon
        {
            get => _hoaDon;
            set { _hoaDon = value; OnPropertyChanged(); }
        }

        public ICommand ThoatCommand { get; }

        public HoaDonChiTietViewModel(int maChiTietDatPhong)
        {
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());

            // Gọi DAL lấy dữ liệu
            var dal = new HoaDonDAL();
            HoaDon = dal.LayChiTietHoaDonTheoPhong(maChiTietDatPhong);
        }
    }
}

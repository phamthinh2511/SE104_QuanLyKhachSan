using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.PhongVM;
using PhongModel = QuanLyKhachSan_SE104.Model.Phong;
using ChiTietDatPhongModel = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;

namespace QuanLyKhachSan_SE104.View.Phong
{
    public partial class ChiTietPhong : Window
    {
        public ChiTietPhong(PhongModel phong, ChiTietDatPhongModel chiTietDatPhong = null)
        {
            InitializeComponent();
            DataContext = new ChiTietPhongViewModel(phong, chiTietDatPhong, this);
        }
    }
}
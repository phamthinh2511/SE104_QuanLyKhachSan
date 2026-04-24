using QuanLyKhachSan_SE104.View.DatPhong;
using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhong;
using QuanLyKhachSan_SE104.ViewModel.DatPhong;
using QuanLyKhachSan_SE104.ViewModel.MainViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QuanLyKhachSan_SE104.View.ChiTietDatPhong
{
    public partial class ChiTietDatPhongPage : UserControl
    {
        public ChiTietDatPhongPage()
        {
            InitializeComponent();
            var vm = new ChiTietDatPhongListViewModel();
            this.DataContext = vm;

            // 1. Lắng nghe sự kiện chuyển sang trang Đặt Phòng
            vm.NavigateToDatPhong += () =>
            {
                // 1. Tạo một "cái vỏ" Window trực tiếp bằng code
                Window popupWindow = new Window
                {
                    Title = "Thông Tin Đặt Phòng",
                    Width = 1100,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    // 2. Gán nội dung là một Instance mới của DatPhongPage
                    Content = new DatPhongPage()
                };

                // 3. Hiển thị nó lên
                popupWindow.ShowDialog();
            };

            // 2. Lắng nghe sự kiện mở cửa sổ Chi Tiết
            vm.OpenChiTietWindow += (datPhongDto) =>
            {
                ChiTietDatPhongWindow window = new ChiTietDatPhongWindow(datPhongDto);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            };

        }
    }
}
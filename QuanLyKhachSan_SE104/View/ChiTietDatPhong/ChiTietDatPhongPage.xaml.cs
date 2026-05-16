using QuanLyKhachSan_SE104.View.DatPhong;
using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhong;
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
            this.DataContext = new DatPhongListViewModel();

        }
    }
}
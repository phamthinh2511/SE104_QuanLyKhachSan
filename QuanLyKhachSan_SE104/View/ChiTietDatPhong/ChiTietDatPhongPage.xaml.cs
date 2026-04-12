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


using QuanLyKhachSan_SE104.ViewModel.DatPhong;

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
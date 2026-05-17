using QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM;
using System.Windows.Controls;

namespace QuanLyKhachSan_SE104.View.ChiTietDatPhongView
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
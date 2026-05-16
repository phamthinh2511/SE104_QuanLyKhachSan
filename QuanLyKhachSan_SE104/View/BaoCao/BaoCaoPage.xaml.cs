using QuanLyKhachSan_SE104.ViewModel.BaoCaoVM;
using System.Windows.Controls;

namespace QuanLyKhachSan_SE104.View.BaoCao
{
    public partial class BaoCaoPage : UserControl
    {
        public BaoCaoPage()
        {
            InitializeComponent();
            this.DataContext = new BaoCaoViewModel();
        }
    }
}
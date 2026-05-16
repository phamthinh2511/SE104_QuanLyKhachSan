using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.View.DatPhong
{
    public partial class DatPhongPage : UserControl
    {
        public DatPhongPage()
        {
            InitializeComponent();
            this.DataContext = new DatPhongViewModel();
        }

        private void RoomItem_Click(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null) return;
            item.IsSelected = !item.IsSelected;
        }
    }
}
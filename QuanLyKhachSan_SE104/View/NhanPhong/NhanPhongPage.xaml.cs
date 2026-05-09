using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_SE104.ViewModel;
using QuanLyKhachSan_SE104.View.ChiTietDatPhong;

// Alias tránh conflict
using ModelChiTiet = QuanLyKhachSan_SE104.Model.ChiTietDatPhong;

namespace QuanLyKhachSan_SE104.View.NhanPhong
{
    public partial class NhanPhongPage : UserControl
    {
        private readonly NhanPhongViewModel _vm;

        public NhanPhongPage()
        {
            InitializeComponent();
            _vm = new NhanPhongViewModel();
            this.DataContext = _vm;

            // Refresh data every time this page becomes visible
            this.IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue == true)
                    _vm.Refresh();
            };
        }

        //private void ShowPopup_Click(object sender, RoutedEventArgs e)
        //{
        //    PopupOverlay.Visibility = Visibility.Visible;
        //}

        //private void ClosePopup_Click(object sender, RoutedEventArgs e)
        //{
        //    PopupOverlay.Visibility = Visibility.Collapsed;
        //}

        //private void ShowChiTiet_Click(object sender, RoutedEventArgs e)
        //{
        //    if (sender is Button btn && btn.Tag is ModelChiTiet chiTiet)
        //    {
        //        if (chiTiet.DatPhong == null) return;

        //        var win = new ChiTietDatPhongWindow(chiTiet.DatPhong)
        //        {
        //            Owner = Window.GetWindow(this)
        //        };
        //        win.ShowDialog();
        //    }
        //}
    }
}
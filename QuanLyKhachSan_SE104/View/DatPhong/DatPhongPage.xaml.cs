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
using System.Windows.Navigation;
using System.Windows.Shapes;
//using QuanLyKhachSan_SE104.ViewModel.DatPhong; 

namespace QuanLyKhachSan_SE104.View.DatPhong
{
    public partial class DatPhongPage : UserControl
    {
        public DatPhongPage()
        {
            InitializeComponent();

            var vm = new DatPhongViewModel();
            this.DataContext = vm;

            // Wire CloseAction now that we have the VM directly —
            // no need to wait for the Loaded event anymore.
            vm.CloseAction = () =>
            {
                Window parentWindow = Window.GetWindow(this);
                parentWindow?.Close();
            };
        }

        private void RoomItem_Click(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null) return;

            if (item.IsSelected)
            {
                item.IsSelected = false; 
            }
            else
            {
                item.IsSelected = true;
            }
        }
    }
}

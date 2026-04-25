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
using QuanLyKhachSan_SE104.ViewModel.DanhMuc;

namespace QuanLyKhachSan_SE104.View.DanhMuc
{
    /// <summary>
    /// Interaction logic for DanhMucCRUD.xaml
    /// </summary>
    public partial class DanhMucCRUD : Window
    {
        public DanhMucCRUD(DanhMucCRUDViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            vm.CloseAction = this.Close;
        }
    }
}
    

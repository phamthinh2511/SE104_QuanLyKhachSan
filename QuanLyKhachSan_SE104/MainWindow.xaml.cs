using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.MainViewModel;

namespace QuanLyKhachSan_SE104
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }
    }
}
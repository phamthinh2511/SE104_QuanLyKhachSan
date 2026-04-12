using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.PhongVM;

namespace QuanLyKhachSan_SE104.View.Phong
{
    /// <summary>
    /// Interaction logic for PhongModal.xaml
    /// </summary>
    public partial class PhongModal : Window
    {
        public PhongModal()
        {
            InitializeComponent();
            this.DataContext = new PhongModalViewModel();
        }
        private void Button_Close(object sender, RoutedEventArgs e) 
        {
            this.Close();
        }
        private void Button_Save(object sender, RoutedEventArgs e)
        {
            try 
            { 

            }
            catch 
            {
                
            }
        }

        private void TextBox_Initialized(object sender, EventArgs e)
        {

        }
    }
}

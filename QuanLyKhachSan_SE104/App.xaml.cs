using System.Windows;
using QuanLyKhachSan_SE104.View.Login;

namespace QuanLyKhachSan_SE104
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true)
            {
                var main = new MainWindow();
                main.Show();
            }
            else
            {
                this.Shutdown();
            }
        }
    }
}

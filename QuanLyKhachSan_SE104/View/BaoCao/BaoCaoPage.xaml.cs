using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace QuanLyKhachSan_SE104.View.BaoCao
{
    public partial class BaoCaoPage : UserControl
    {
        public BaoCaoPage()
        {
            InitializeComponent();
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (TabDoanhThu == null || TabBanPhong == null || TabDichVu == null || TabNangSuat == null) return;

            var rb = sender as RadioButton;
            if (rb == null) return;

            TabDoanhThu.Visibility = Visibility.Collapsed;
            TabBanPhong.Visibility = Visibility.Collapsed;
            TabDichVu.Visibility = Visibility.Collapsed;
            TabNangSuat.Visibility = Visibility.Collapsed;

            string tag = rb.Tag?.ToString();
            if (tag == "TabDoanhThu") TabDoanhThu.Visibility = Visibility.Visible;
            else if (tag == "TabBanPhong") TabBanPhong.Visibility = Visibility.Visible;
            else if (tag == "TabDichVu") TabDichVu.Visibility = Visibility.Visible;
            else if (tag == "TabNangSuat") TabNangSuat.Visibility = Visibility.Visible;
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "BaoCao_KhachSan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF documents (.pdf)|*.pdf";

                if (dlg.ShowDialog() == true)
                {
                    // Chụp ảnh màn hình khu vực báo cáo (printContainer)
                    RenderTargetBitmap rtb = new RenderTargetBitmap(
                        (int)printContainer.ActualWidth, 
                        (int)printContainer.ActualHeight, 
                        96, 96, PixelFormats.Pbgra32);
                        
                    rtb.Render(printContainer);

                    // Convert ra Png memory stream
                    MemoryStream stream = new MemoryStream();
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(stream);
                    byte[] imageBytes = stream.ToArray();

                    // Tạo file PDF
                    PdfDocument document = new PdfDocument();
                    PdfPage page = document.AddPage();
                    
                    // Để hình ảnh vừa vặn, set kích thước trang PDF bằng kích thước ảnh
                    page.Width = printContainer.ActualWidth;
                    page.Height = printContainer.ActualHeight;

                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    
                    // PdfSharpCore cần Func trả về stream
                    XImage image = XImage.FromStream(() => new MemoryStream(imageBytes));
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                    document.Save(dlg.FileName);
                    MessageBox.Show("Xuất PDF thành công!\nFile được lưu tại: " + dlg.FileName, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi khi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

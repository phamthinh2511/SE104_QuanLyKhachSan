using System.Windows;

namespace QuanLyKhachSan_SE104.View.HoaDon
{
    public partial class PaymentMethodDialog : Window
    {
        /// <summary>
        /// 0 = Tiền mặt, 1 = Thẻ tín dụng, 2 = Chuyển khoản
        /// Only valid when DialogResult == true.
        /// </summary>
        public int SelectedMethod { get; private set; } = 0;

        public PaymentMethodDialog()
        {
            InitializeComponent();
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            if (RadioCash.IsChecked == true) SelectedMethod = 0;
            else if (RadioCard.IsChecked == true) SelectedMethod = 1;
            else if (RadioTransfer.IsChecked == true) SelectedMethod = 2;

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
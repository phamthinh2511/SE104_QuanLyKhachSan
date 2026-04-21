using QuanLyKhachSan_SE104.DAL;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel
{
    public class NhanPhongViewModel : INotifyPropertyChanged
    {
        private NhanPhongDAL _dal;

        // ==========================================
        // 1. CÁC PROPERTY BINDING LÊN UI (XAML)
        // ==========================================

        private ObservableCollection<ThongTinNhanPhongDTO> _upcomingCheckins;
        public ObservableCollection<ThongTinNhanPhongDTO> UpcomingCheckins
        {
            get => _upcomingCheckins;
            set { _upcomingCheckins = value; OnPropertyChanged(nameof(UpcomingCheckins)); }
        }

        // Quản lý ẩn/hiện Popup Xác nhận Nhận phòng (Ảnh 3)
        private bool _isCheckInPopupVisible;
        public bool IsCheckInPopupVisible
        {
            get => _isCheckInPopupVisible;
            set { _isCheckInPopupVisible = value; OnPropertyChanged(nameof(IsCheckInPopupVisible)); }
        }

        // Quản lý ẩn/hiện Popup Chi tiết (Ảnh 2)
        private bool _isDetailPopupVisible;
        public bool IsDetailPopupVisible
        {
            get => _isDetailPopupVisible;
            set { _isDetailPopupVisible = value; OnPropertyChanged(nameof(IsDetailPopupVisible)); }
        }

        // Lưu thông tin của phòng đang được chọn thao tác
        private ThongTinNhanPhongDTO _selectedBooking;
        public ThongTinNhanPhongDTO SelectedBooking
        {
            get => _selectedBooking;
            set { _selectedBooking = value; OnPropertyChanged(nameof(SelectedBooking)); }
        }

        // Giá trị binding vào TextBox Số người thực tế trong Popup
        private int _soNguoiThucTe;
        public int SoNguoiThucTe
        {
            get => _soNguoiThucTe;
            set { _soNguoiThucTe = value; OnPropertyChanged(nameof(SoNguoiThucTe)); }
        }

        // Giá trị binding vào TextBox CCCD/Passport trong Popup
        private string _cccdThucTe;
        public string CccdThucTe
        {
            get => _cccdThucTe;
            set { _cccdThucTe = value; OnPropertyChanged(nameof(CccdThucTe)); }
        }

        // ==========================================
        // 2. KHAI BÁO CÁC COMMAND
        // ==========================================
        public ICommand ShowCheckInPopupCommand { get; set; }
        public ICommand ClosePopupCommand { get; set; }
        public ICommand ConfirmCheckInCommand { get; set; }
        public ICommand ShowChiTietCommand { get; set; }
        public ICommand CloseChiTietCommand { get; set; }

        // ==========================================
        // 3. CONSTRUCTOR & KHỞI TẠO
        // ==========================================
        public NhanPhongViewModel()
        {
            _dal = new NhanPhongDAL();

            // Khởi tạo các RelayCommand
            ShowCheckInPopupCommand = new RelayCommand<ThongTinNhanPhongDTO>(OpenCheckInPopup);
            ClosePopupCommand = new RelayCommand<object>((p) =>
            {
                IsCheckInPopupVisible = false;
                IsDetailPopupVisible = false;
            });
            ConfirmCheckInCommand = new RelayCommand<object>(ExecuteCheckIn);
            ShowChiTietCommand = new RelayCommand<ThongTinNhanPhongDTO>(OpenChiTietPopup);

            // Load dữ liệu lần đầu
            LoadData();
        }

        // ==========================================
        // 4. CÁC HÀM XỬ LÝ LOGIC (NGHIỆP VỤ)
        // ==========================================

        private void LoadData()
        {
            try
            {
                var data = _dal.LayDanhSachNhanPhongDuKien();
                UpcomingCheckins = new ObservableCollection<ThongTinNhanPhongDTO>(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách nhận phòng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCheckInPopup(ThongTinNhanPhongDTO booking)
        {
            if (booking == null) return;

            SelectedBooking = booking;

            // Tự động điền dữ liệu dự kiến vào Textbox để người dùng đỡ phải gõ lại
            SoNguoiThucTe = booking.SoNguoi;
            CccdThucTe = booking.CCCD_Passport;

            // Hiển thị popup
            IsCheckInPopupVisible = true;
        }

        private void ExecuteCheckIn(object parameter)
        {
            if (SelectedBooking == null) return;

            // Validate dữ liệu nhập (Ví dụ cơ bản)
            if (SoNguoiThucTe <= 0)
            {
                MessageBox.Show("Số người thực tế phải lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CccdThucTe))
            {
                MessageBox.Show("Vui lòng nhập số CCCD/Passport của khách hàng.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Gọi xuống DAL để thực thi ghi vào database
            bool success = _dal.XacNhanNhanPhong(
                SelectedBooking.MaChiTietDatPhong,
                SelectedBooking.MaPhong,
                SelectedBooking.MaKhachHang,
                CccdThucTe,
                SoNguoiThucTe,
                SelectedBooking.MaDatPhong);

            if (success)
            {
                MessageBox.Show($"Đã xác nhận nhận phòng thành công cho khách {SelectedBooking.TenKhachHang} - Phòng {SelectedBooking.TenPhong}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh lại danh sách (phòng đã nhận sẽ biến mất khỏi danh sách chờ)
                LoadData();

                // Đóng Popup
                IsCheckInPopupVisible = false;
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra trong quá trình nhận phòng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenChiTietPopup(ThongTinNhanPhongDTO booking)
        {
            if (booking == null) return;
            SelectedBooking = booking;

            // Mở popup Chi tiết phiếu thuê
            IsDetailPopupVisible = true;
        }

        // ==========================================
        // 5. IMPLEMENT INotifyPropertyChanged
        // ==========================================
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
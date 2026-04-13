using System.Windows;
using QuanLyKhachSan_SE104.ViewModel.PhongVM;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.View.PhongView
{
    /// <summary>
    /// Interaction logic for PhongModal.xaml
    /// </summary>
    public partial class PhongModal : Window
    {
        private PhongViewModel _phong;

        public Phong PhongInfo { get; set; }

        public List<LoaiPhong> DanhSachLoaiPhong { get; set; }

        private Phong _originPhong;

        public PhongModal(Phong p = null)
        {
            InitializeComponent();
            using (var context = new QuanLyKhachSanContext())
            {
                this.DanhSachLoaiPhong = context.LoaiPhongs.ToList();
            }
            // Tạo mới phòng
            if (p == null)
            {
                Phong PhongInfo = new Phong();
            }
            // Sửa bằng cách truyền tham số trung gian
            else
            {
                PhongInfo = p;
                _originPhong = new Phong 
                {
                    MaPhong = p.MaPhong,
                    TenPhong = p.TenPhong,
                    LoaiPhong = p.LoaiPhong,
                    SoTang = p.SoTang,
                    TrangThai = p.TrangThai,
                    TrangThaiDonDep = p.TrangThaiDonDep
                };
            }
            // Kết nối D.Liệu
            this.DataContext = new PhongModalViewModel(PhongInfo, DanhSachLoaiPhong);
        }
        private bool IsDataChanged()
        {
            if (_originPhong == null)
                return false;

            var vm = this.DataContext as PhongModalViewModel;
            if (vm == null) return false;

            // So sánh qua ViewModel thay vì lấy UI Textbox (Chuẩn MVVM)
            if (vm.MaPhong != _originPhong.MaPhong) return true;
            if (vm.TenPhong?.Trim() != _originPhong.TenPhong?.Trim()) return true;
            if (vm.SoTang != _originPhong.SoTang) return true;
            if (vm.Loaiphong?.MaLoaiPhong != _originPhong.MaLoaiPhong) return true;
            if (vm.TrangThaiValue != _originPhong.TrangThai) return true;
            if (vm.TrangThaiDonDepValue != _originPhong.TrangThaiDonDep) return true;

            return false;
        }
        private void button_Close(object sender, RoutedEventArgs e) 
        {
            this.Close();
        }
        private void button_Save(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = this.DataContext as PhongModalViewModel;
                if (vm == null || string.IsNullOrWhiteSpace(vm.TenPhong) || vm.Loaiphong == null)
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ tên phòng và loại phòng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new QuanLyKhachSanContext())
                {
                    if (_originPhong == null)
                    {
                        // Thêm mới
                        var newPhong = new Phong
                        {
                            TenPhong = vm.TenPhong.Trim(),
                            MaLoaiPhong = vm.Loaiphong.MaLoaiPhong,
                            SoTang = vm.SoTang,
                            TrangThai = vm.TrangThaiValue,
                            TrangThaiDonDep = vm.TrangThaiDonDepValue
                        };
                        context.Phongs.Add(newPhong);
                    }
                    else
                    {
                        // Cập nhật
                        var updatePhong = context.Phongs.FirstOrDefault(p => p.MaPhong == vm.MaPhong);
                        if (updatePhong != null)
                        {
                            updatePhong.TenPhong = vm.TenPhong.Trim();
                            updatePhong.MaLoaiPhong = vm.Loaiphong.MaLoaiPhong;
                            updatePhong.SoTang = vm.SoTang;
                            updatePhong.TrangThai = vm.TrangThaiValue;
                            updatePhong.TrangThaiDonDep = vm.TrangThaiDonDepValue;
                        }
                    }
                    context.SaveChanges();
                }

                MessageBox.Show("Lưu thông tin phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Trả về true cho Window cha biết là đã thay đổi Data
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RestoreOriginalData()
        {
            if (_originPhong != null)
            {
                var vm = this.DataContext as PhongModalViewModel;
                if (vm != null)
                {
                    vm.MaPhong = _originPhong.MaPhong;
                    vm.TenPhong = _originPhong.TenPhong;
                    vm.SoTang = _originPhong.SoTang;
                    vm.Loaiphong = vm.DanhSachLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == _originPhong.MaLoaiPhong);
                    vm.TrangThaiValue = _originPhong.TrangThai;
                    vm.TrangThaiDonDepValue = _originPhong.TrangThaiDonDep;
                }
            }
        }
        private void button_Cancel(object sender, RoutedEventArgs e)
        {
            // ===== 1. Không có thay đổi =====
            if (!IsDataChanged())
            {
                MessageBox.Show(
                    "Chưa có dữ liệu nào để đặt lại",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // ===== 2. Có thay đổi → hỏi =====
            var result = MessageBox.Show(
                "Bạn có muốn đặt lại dữ liệu về trạng thái đã lưu gần nhất không?",
                "Xác nhận đặt lại",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            // ===== 3. OK → đặt lại =====
            if (result == MessageBoxResult.OK)
            {
                RestoreOriginalData();
                MessageBox.Show(
                    "Dữ liệu đã được đặt lại về trạng thái đã lưu.",
                    "Hoàn tất",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            // Cancel → không làm gì
        }
    }
}

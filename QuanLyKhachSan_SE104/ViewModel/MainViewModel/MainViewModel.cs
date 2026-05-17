using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View;
using QuanLyKhachSan_SE104.View.BaoCao;
using QuanLyKhachSan_SE104.View.Dashboard;
using QuanLyKhachSan_SE104.View.DatPhong;
using QuanLyKhachSan_SE104.View.DichVu;
using QuanLyKhachSan_SE104.View.HoaDon;
using QuanLyKhachSan_SE104.View.KhachHang;
using QuanLyKhachSan_SE104.View.NhanVienView;
using QuanLyKhachSan_SE104.View.PhongView;
using QuanLyKhachSan_SE104.View.SuDungDichVu;
using QuanLyKhachSan_SE104.View.NhanPhong;
using QuanLyKhachSan_SE104.View.ChiTietDatPhongView;
using QuanLyKhachSan_SE104.View.DanhMuc;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;


namespace QuanLyKhachSan_SE104.ViewModel.MainViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ── Danh mục hardcode ─────────────────────────────
        public ObservableCollection<NavigationItem> Pages { get; }
            = new ObservableCollection<NavigationItem>();

        // ── Trang hiện tại ────────────────────────────────
        private NavigationItem _currentPage;
        public NavigationItem CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != null) _currentPage.IsSelected = false;
                _currentPage = value;
                if (_currentPage != null) _currentPage.IsSelected = true;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentTitle));
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        public string CurrentTitle => _currentPage?.Title ?? "";
        public UserControl CurrentContent => _currentPage?.PageContent;

        // ── Command ───────────────────────────────────────
        public ICommand SelectPageCommand { get; }

        public MainViewModel()
        {
            SelectPageCommand = new RelayCommand<NavigationItem>(p => CurrentPage = p);

            // ════════════════════════════════════════════════
            // Trang được tạo LAZY (chỉ khi người dùng click vào lần đầu)
            // để tránh crash khi khởi động nếu một trang nào đó lỗi.
            // ════════════════════════════════════════════════
            Pages.Add(new NavigationItem
            {
                Icon = "🏠",
                Title = "Tổng quan",
                BadgeCount = 0,
                PageFactory = () => new DashboardPage()
            });

            Pages.Add(new NavigationItem
            {
                Icon = "🛏",
                Title = "Phòng",
                BadgeCount = 3,
                PageFactory = () => new PhongPage()
            });

            Pages.Add(new NavigationItem
            {
                Icon = "📅",
                Title = "Đặt phòng",
                BadgeCount = 12,
                PageFactory = () => new DatPhongPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "📅",
                Title = "Chi Tiết Đặt phòng",
                BadgeCount = 12,
                PageFactory = () => new ChiTietDatPhongPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "🔑",
                Title = "Nhận phòng",
                BadgeCount = 1,
                PageFactory = () => new NhanPhongPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "📋",
                Title = "Trả Phòng",
                BadgeCount = 1,
                PageFactory = () => new TraPhongPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "👥",
                Title = "Khách hàng",
                BadgeCount = 0,
                PageFactory = () => new KhachHangPage()
            });

            Pages.Add(new NavigationItem
            {
                Icon = "💰",
                Title = "Hóa Đơn",
                BadgeCount = 0,
                PageFactory = () => new HoaDonPage()
            });

            Pages.Add(new NavigationItem
            {
                Icon = "👔",
                Title = "Nhân Viên",
                BadgeCount = 1,
                PageFactory = () => new NhanVienPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "📊",
                Title = "Báo Cáo",
                BadgeCount = 0,
                PageFactory = () => new BaoCaoPage()
            });
            Pages.Add(new NavigationItem
            {
                Icon = "📋",
                Title = "Danh Mục",
                BadgeCount = 0,
                PageFactory = () => new QuanLyKhachSan_SE104.View.DanhMuc.DanhMuc()
            });

            // Chọn trang đầu tiên mặc định
            CurrentPage = Pages[0];

            //try
            //{
            //    var db = new QuanLyKhachSanContext();
            //    var count = db.TaiKhoans.Count();
            //    System.Windows.MessageBox.Show(count.ToString());
            //}
            //catch (Exception ex)
            //{
            //    System.Windows.MessageBox.Show("Lỗi kết nối: " + ex.Message);
            //}
        }

        // ── Cập nhật badge từ code (gọi khi có data mới) ──
        // Ví dụ: _vm.UpdateBadge("Đặt phòng", 15);
        public void UpdateBadge(string pageTitle, int count)
        {
            foreach (var p in Pages)
                if (p.Title == pageTitle) { p.BadgeCount = count; break; }
        }

        private string _currentUsername = "Admin";
        public string CurrentUsername
        {
            get => _currentUsername;
            set
            {
                _currentUsername = value;
                OnPropertyChanged(nameof(CurrentUsername));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

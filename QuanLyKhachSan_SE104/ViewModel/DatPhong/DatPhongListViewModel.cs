using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.DatPhong;
using QuanLyKhachSan_SE104.View.ChiTietDatPhong;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;

using ModelDatPhong = QuanLyKhachSan_SE104.Model.DatPhong;

namespace QuanLyKhachSan_SE104.ViewModel.DatPhong
{
    public class DatPhongListViewModel : INotifyPropertyChanged
    {
        private List<ModelDatPhong> _allBookings;
        private QuanLyKhachSanContext _context;

        // 1. Khớp với ItemsSource="{Binding ListDatPhong}"
        private ObservableCollection<ModelDatPhong> _listDatPhong;
        public ObservableCollection<ModelDatPhong> ListDatPhong
        {
            get => _listDatPhong;
            set { _listDatPhong = value; OnPropertyChanged(); }
        }

        // 2. Khớp với Text="{Binding SearchText}"
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ExecuteSearch();
            }
        }

        // 3. Khớp với các Command trong XAML
        public ICommand DatPhongCommand { get; }    // Nút "Đặt phòng" màu xanh
        public ICommand XemChiTietCommand { get; }  // Nút "..."
        public ICommand XoaCommand { get; }         // Nút "✕"

        public DatPhongListViewModel()
        {
            _context = new QuanLyKhachSanContext();

            // Khởi tạo các Command với tên khớp XAML
            DatPhongCommand = new RelayCommand<object>(OpenCreateBooking);
            //XemChiTietCommand = new RelayCommand<ModelDatPhong>(OpenBooking);
            XoaCommand = new RelayCommand<ModelDatPhong>(DeleteBooking);

            LoadData();
        }

        private void LoadData()
        {
            // Sử dụng một context duy nhất hoặc khởi tạo mới tùy kiến trúc, 
            // nhưng nên load đầy đủ Include để tránh lỗi Binding nested property (KhachHang.HoTen)
            var data = _context.DatPhongs
                .Include(x => x.KhachHang)
                .Include(x => x.NhanVien)
                .Include(x => x.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                .ToList();

            _allBookings = data;
            ListDatPhong = new ObservableCollection<ModelDatPhong>(_allBookings);
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListDatPhong = new ObservableCollection<ModelDatPhong>(_allBookings);
                return;
            }

            var keyword = SearchText.ToLower();
            var result = _allBookings.Where(x =>
                x.MaDatPhong.ToString().Contains(keyword) ||
                (x.KhachHang != null && x.KhachHang.HoTen.ToLower().Contains(keyword))
            ).ToList();

            ListDatPhong = new ObservableCollection<ModelDatPhong>(result);
        }

        //private void OpenBooking(ModelDatPhong booking)
        //{
        //    if (booking == null) return;

        //    // Truyền booking sang Window chi tiết
        //    var window = new ChiTietDatPhongWindow(booking);
        //    window.ShowDialog();

        //    LoadData(); // Load lại sau khi đóng popup nếu có sửa đổi
        //}

        private void DeleteBooking(ModelDatPhong booking)
        {
            if (booking == null) return;

            var confirm = MessageBox.Show(
                $"Xóa phiếu #{booking.MaDatPhong}?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var entity = _context.DatPhongs.Find(booking.MaDatPhong);
                    if (entity != null)
                    {
                        _context.DatPhongs.Remove(entity);
                        _context.SaveChanges();
                        LoadData();
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Không thể xóa: " + ex.Message);
                }
            }
        }

        private void OpenCreateBooking(object obj)
        {
            var window = new Window
            {
                Title = "Đặt phòng mới",
                Width = 1100,
                Height = 700,
                Content = new DatPhongPage(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
            LoadData();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
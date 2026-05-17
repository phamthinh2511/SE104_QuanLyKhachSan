using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.ChiTietDatPhongView;

//// Quản lý màn hình danh sách bên ngoài cùng 
namespace QuanLyKhachSan_SE104.ViewModel.ChiTietDatPhongVM
{
    public class DatPhongListViewModel : INotifyPropertyChanged
    {
        private readonly DatPhongDAL _dal = new DatPhongDAL();
        private ObservableCollection<DatPhongDTO> _allBookings = new ObservableCollection<DatPhongDTO>();

        // Danh sách hiển thị lên bảng (DataGrid/ItemsControl)
        private ObservableCollection<DatPhongDTO> _listDatPhong = new ObservableCollection<DatPhongDTO>();
        public ObservableCollection<DatPhongDTO> ListDatPhong
        {
            get => _listDatPhong;
            set { _listDatPhong = value; OnPropertyChanged(); }
        }

        // Thanh tìm kiếm Real-time
        private string _searchText = string.Empty;
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

        // Chỉ giữ lại nút xem chi tiết (...)
        public ICommand XemChiTietCommand { get; }

        public DatPhongListViewModel()
        {
            XemChiTietCommand = new RelayCommand<DatPhongDTO>(ExecuteXemChiTiet);
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                // Lấy data bằng DAL thuần
                var data = _dal.LayDanhSachDatPhong();
                _allBookings = new ObservableCollection<DatPhongDTO>(data);
                ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListDatPhong = new ObservableCollection<DatPhongDTO>(_allBookings);
                return;
            }

            var keyword = SearchText.Trim().ToLower();
            var filtered = _allBookings.Where(x =>
                x.MaDatPhong.ToString().Contains(keyword) ||
                (x.TenKhachHang != null && x.TenKhachHang.ToLower().Contains(keyword))
            );
            ListDatPhong = new ObservableCollection<DatPhongDTO>(filtered);
        }

        private void ExecuteXemChiTiet(DatPhongDTO item)
        {
            if (item == null) return;
            try
            {
                // Gọi DAL lấy danh sách các phòng thuộc phiếu đặt này
                item.DanhSachChiTiet = _dal.LayChiTietCacPhong(item.MaDatPhong);

                // Khởi tạo ViewModel
                var vm = new ChiTietDatPhongWindowViewModel(item);

                // Khởi tạo Window và truyền ViewModel vào
                var window = new ChiTietDatPhongWindow(vm);

                // Ràng buộc lệnh đóng cửa sổ
                vm.RequestClose += () => window.Close();

                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
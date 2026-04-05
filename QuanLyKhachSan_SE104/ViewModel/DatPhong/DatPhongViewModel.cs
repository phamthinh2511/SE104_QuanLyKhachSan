using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.ViewModel.DatPhong
{
    public class DatPhongViewModel : INotifyPropertyChanged
    {
        // 1. Danh sách hiển thị lên View
        private ObservableCollection<Phong> _availableRooms;
        public ObservableCollection<Phong> AvailableRooms
        {
            get => _availableRooms;
            set { _availableRooms = value; OnPropertyChanged(); }
        }

        // Danh sách cho ComboBox (Sử dụng backing field để an toàn hơn)
        private ObservableCollection<int> _listTang;
        public ObservableCollection<int> ListTang
        {
            get => _listTang;
            set { _listTang = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LoaiPhong> _listLoaiPhong;
        public ObservableCollection<LoaiPhong> ListLoaiPhong
        {
            get => _listLoaiPhong;
            set { _listLoaiPhong = value; OnPropertyChanged(); }
        }

        // Đối tượng hứng dữ liệu
        public Model.KhachHang NewCustomer { get; set; } = new Model.KhachHang();

        private Phong _selectedRoom;
        public Phong SelectedRoom
        {
            get => _selectedRoom;
            set { _selectedRoom = value; OnPropertyChanged(); }
        }

        public ICommand SearchAvailableRoomsCommand { get; set; }

        public DatPhongViewModel()
        {
            // Quan trọng: Khởi tạo danh sách trống trước khi Load dữ liệu
            AvailableRooms = new ObservableCollection<Phong>();
            ListTang = new ObservableCollection<int>();
            ListLoaiPhong = new ObservableCollection<LoaiPhong>();

            SearchAvailableRoomsCommand = new RelayCommand<object>(
                (p) =>
                {
                    MessageBox.Show("Đang tìm phòng...");
                },
                (p) => true 
            );

            LoadMockData();
        }

        void LoadMockData()
        {
            // Dữ liệu mẫu cho Loại Phòng (Để ComboBox Loại phòng không bị trắng)
            var lpStandard = new LoaiPhong { TenLoaiPhong = "Standard", GiaMacDinh = 200000 };
            var lpDeluxe = new LoaiPhong { TenLoaiPhong = "Deluxe", GiaMacDinh = 500000 };
            var lpSuite = new LoaiPhong { TenLoaiPhong = "Suite", GiaMacDinh = 1200000 };

            ListLoaiPhong.Add(lpStandard);
            ListLoaiPhong.Add(lpDeluxe);
            ListLoaiPhong.Add(lpSuite);

            // Dữ liệu mẫu cho Tầng
            ListTang.Add(1);
            ListTang.Add(2);
            ListTang.Add(3);

            // Dữ liệu mẫu cho Phòng
            AvailableRooms.Add(new Phong { TenPhong = "101", SoTang = 1, LoaiPhong = lpStandard });
            AvailableRooms.Add(new Phong { TenPhong = "102", SoTang = 1, LoaiPhong = lpStandard });
            AvailableRooms.Add(new Phong { TenPhong = "201", SoTang = 2, LoaiPhong = lpDeluxe });
            AvailableRooms.Add(new Phong { TenPhong = "305", SoTang = 3, LoaiPhong = lpSuite });
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.DatPhong
{
    public class DatPhongViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private QuanLyKhachSanContext _context;

        #region Properties cho UI Binding
        private KhachHang _newCustomer = new KhachHang();
        public KhachHang NewCustomer
        {
            get => _newCustomer;
            set { _newCustomer = value; OnPropertyChanged(); }
        }

        private DateTime _ngayCheckIn = DateTime.Now;
        public DateTime NgayCheckIn
        {
            get => _ngayCheckIn;
            set { _ngayCheckIn = value; OnPropertyChanged(); }
        }

        private DateTime _ngayCheckOut = DateTime.Now.AddDays(1);
        public DateTime NgayCheckOut
        {
            get => _ngayCheckOut;
            set { _ngayCheckOut = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Phong> _availableRooms;
        public ObservableCollection<Phong> AvailableRooms
        {
            get => _availableRooms;
            set { _availableRooms = value; OnPropertyChanged(); }
        }

        private Phong _selectedRoom;
        public Phong SelectedRoom
        {
            get => _selectedRoom;
            set { _selectedRoom = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ListGioiTinh { get; set; }

        // Các danh sách từ DB
        public ObservableCollection<int> ListTang { get; set; }
        public ObservableCollection<LoaiPhong> ListLoaiPhong { get; set; }

        private int _selectedTang;
        public int SelectedTang { get => _selectedTang; set { _selectedTang = value; OnPropertyChanged(); } }

        private LoaiPhong _selectedLoaiPhong;
        public LoaiPhong SelectedLoaiPhong { get => _selectedLoaiPhong; set { _selectedLoaiPhong = value; OnPropertyChanged(); } }

        #endregion

        public ICommand SearchRoomsCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public DatPhongViewModel()
        {
            _context = new QuanLyKhachSanContext();
            AvailableRooms = new ObservableCollection<Phong>();

            // --- KHỞI TẠO DỮ LIỆU ---

            // Khởi tạo List giới tính (Fix lỗi ComboBox giới tính trống)
            ListGioiTinh = new ObservableCollection<string> { "Nam", "Nữ"};

            // Nạp dữ liệu từ DB (Fix lỗi ComboBox Tầng và Loại Phòng trống)
            LoadInitialData();

            SearchRoomsCommand = new RelayCommand(ExecuteSearchRooms);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        }

        private void LoadInitialData()
        {
            try
            {
                // Lấy danh sách tầng duy nhất từ bảng Phòng (hoặc từ bảng Tang nếu bạn có bảng riêng)
                var tangs = _context.Phongs
                    .Select(p => p.SoTang)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
                ListTang = new ObservableCollection<int>(tangs);

                // Lấy danh sách loại phòng
                var loaiPhongs = _context.LoaiPhongs.ToList();
                ListLoaiPhong = new ObservableCollection<LoaiPhong>(loaiPhongs);

                // Thông báo cho UI rằng danh sách đã có dữ liệu
                OnPropertyChanged(nameof(ListTang));
                OnPropertyChanged(nameof(ListLoaiPhong));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu ban đầu: " + ex.Message);
            }
        }

        private void ExecuteSearchRooms()
        {
            // Lấy danh sách ID phòng đã có người đặt trong khoảng thời gian chọn
            var busyRoomIds = _context.ChiTietDatPhongs
                .Where(ct => !(NgayCheckOut <= ct.NgayCheckIn || NgayCheckIn >= ct.NgayCheckOut))
                .Select(ct => ct.MaPhong).ToList();

            // CHỈ Include LoaiPhong (vì LoaiPhong là một Class/Navigation Property)
            // KHÔNG Include SoTang nếu nó là kiểu int/string bình thường
            var query = _context.Phongs.Include(r => r.LoaiPhong).AsQueryable();

            // Lọc theo trạng thái và thời gian
            query = query.Where(r => !busyRoomIds.Contains(r.MaPhong) && r.TrangThai == 0);

            // Lọc theo Tầng (Sử dụng trực tiếp thuộc tính SoTang)
            if (SelectedTang > 0)
            {
                query = query.Where(r => r.SoTang == SelectedTang);
            }

            // Lọc theo Loại phòng
            if (SelectedLoaiPhong != null)
            {
                query = query.Where(r => r.MaLoaiPhong == SelectedLoaiPhong.MaLoaiPhong);
            }

            try
            {
                var result = query.ToList(); // Lỗi thường nổ ra tại đây
                AvailableRooms.Clear();
                foreach (var r in result)
                    AvailableRooms.Add(r);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn SQL: " + ex.Message);
            }
        }

        private bool CanExecuteSave()
        {
            // Nút Save chỉ hiện khi đã chọn phòng và nhập tên khách
            return SelectedRoom != null && !string.IsNullOrEmpty(NewCustomer?.HoTen);
        }

        private void ExecuteSave()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Lưu Khách hàng
                    _context.KhachHangs.Add(NewCustomer);
                    _context.SaveChanges();

                    // 2. Tạo Đơn đặt phòng
                    var booking = new Model.DatPhong
                    {
                        MaKhachHang = NewCustomer.MaKhachHang,
                        NgayDat = DateTime.Now,
                        TrangThaiDat = 1,
                        MaNhanVien = 1, // Nên lấy từ LoginSession thực tế
                        TienCoc = 0
                    };
                    _context.DatPhongs.Add(booking);
                    _context.SaveChanges();

                    // 3. Tạo Chi tiết đặt phòng
                    var detail = new ChiTietDatPhong
                    {
                        MaDatPhong = booking.MaDatPhong,
                        MaPhong = SelectedRoom.MaPhong,
                        NgayCheckIn = this.NgayCheckIn,
                        NgayCheckOut = this.NgayCheckOut,
                        GiaDat = SelectedRoom.LoaiPhong.GiaMacDinh,
                        SoNguoi = 1
                    };
                    _context.ChiTietDatPhongs.Add(detail);

                    _context.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show($"Đặt phòng {SelectedRoom.TenPhong} thành công!", "Thông báo");

                    // Reset Form
                    NewCustomer = new KhachHang();
                    AvailableRooms.Clear();
                    SelectedRoom = null;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Lỗi khi lưu: " + ex.Message);
                }
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.DatPhong
{
    // VM for DatPhongPage.xaml
    // Bindings:
    //   NewCustomer.HoTen / .GioiTinh / .QuocTich / .CCCD_Passport / .SDT / .DiaChi
    //   NgayCheckIn, NgayCheckOut
    //   ListGioiTinh, ListTang, ListLoaiPhong
    //   SelectedTang, SelectedLoaiPhong
    //   SearchRoomsCommand
    //   AvailableRooms (card list)
    //   SelectedRoom
    //   SelectedRoomsList  → RoomName, Capacity
    //   SaveCommand
    public class DatPhongViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly QuanLyKhachSanContext _context;

        // ── Customer form ─────────────────────────────────────────
        private KhachHang _newCustomer = new KhachHang();
        public KhachHang NewCustomer
        {
            get => _newCustomer;
            set { _newCustomer = value; OnPropertyChanged(); }
        }

        // ── Date pickers ──────────────────────────────────────────
        private DateTime _ngayCheckIn = DateTime.Today;
        public DateTime NgayCheckIn
        {
            get => _ngayCheckIn;
            set { _ngayCheckIn = value; OnPropertyChanged(); }
        }

        private DateTime _ngayCheckOut = DateTime.Today.AddDays(1);
        public DateTime NgayCheckOut
        {
            get => _ngayCheckOut;
            set { _ngayCheckOut = value; OnPropertyChanged(); }
        }

        // ── Filter combos ─────────────────────────────────────────
        public ObservableCollection<string> ListGioiTinh { get; set; }
        public ObservableCollection<int> ListTang { get; set; }
        public ObservableCollection<LoaiPhong> ListLoaiPhong { get; set; }

        private int _selectedTang;
        public int SelectedTang
        {
            get => _selectedTang;
            set { _selectedTang = value; OnPropertyChanged(); }
        }

        private LoaiPhong _selectedLoaiPhong;
        public LoaiPhong SelectedLoaiPhong
        {
            get => _selectedLoaiPhong;
            set { _selectedLoaiPhong = value; OnPropertyChanged(); }
        }

        // ── Available rooms (card grid) ───────────────────────────
        private ObservableCollection<Phong> _availableRooms = new();
        public ObservableCollection<Phong> AvailableRooms
        {
            get => _availableRooms;
            set { _availableRooms = value; OnPropertyChanged(); }
        }

        // ── Single selected room (ListBox SelectedItem) ───────────
        private Phong _selectedRoom;
        public Phong SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged();
                // Keep SelectedRoomsList in sync
                SyncSelectedRoomsList();
                //((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Selected rooms list (right panel) ────────────────────
        // Binding: SelectedRoomsList → RoomName, Capacity (TwoWay)
        private ObservableCollection<SelectedRoomItem> _selectedRoomsList = new();
        public ObservableCollection<SelectedRoomItem> SelectedRoomsList
        {
            get => _selectedRoomsList;
            set { _selectedRoomsList = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand SearchRoomsCommand { get; }
        public ICommand SaveCommand { get; }

        public DatPhongViewModel()
        {
            _context = new QuanLyKhachSanContext();

            ListGioiTinh = new ObservableCollection<string> { "Nam", "Nữ" };
            LoadInitialData();

            SearchRoomsCommand = new RelayCommand(ExecuteSearchRooms);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        }

        private void LoadInitialData()
        {
            try
            {
                var tangs = _context.Phongs
                    .Select(p => p.SoTang)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
                ListTang = new ObservableCollection<int>(tangs);

                var loaiPhongs = _context.LoaiPhongs.ToList();
                ListLoaiPhong = new ObservableCollection<LoaiPhong>(loaiPhongs);

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
            try
            {
                var busyRoomIds = _context.ChiTietDatPhongs
                    .Where(ct => !(NgayCheckOut <= ct.NgayCheckIn || NgayCheckIn >= ct.NgayCheckOut))
                    .Select(ct => ct.MaPhong)
                    .ToList();

                var query = _context.Phongs
                    .Include(r => r.LoaiPhong)
                    .Where(r => !busyRoomIds.Contains(r.MaPhong) && r.TrangThai == 0)
                    .AsQueryable();

                if (SelectedTang > 0)
                    query = query.Where(r => r.SoTang == SelectedTang);

                if (SelectedLoaiPhong != null)
                    query = query.Where(r => r.MaLoaiPhong == SelectedLoaiPhong.MaLoaiPhong);

                var result = query.ToList();
                AvailableRooms.Clear();
                foreach (var r in result)
                    AvailableRooms.Add(r);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn: " + ex.Message);
            }
        }

        // Keep the right-panel list in sync whenever SelectedRoom changes.
        // Currently DatPhongPage.xaml supports selecting one room at a time via ListBox.
        // The right panel (SelectedRoomsList) shows that one room with an editable Capacity.
        private void SyncSelectedRoomsList()
        {
            SelectedRoomsList.Clear();
            if (_selectedRoom == null) return;

            SelectedRoomsList.Add(new SelectedRoomItem
            {
                MaPhong = _selectedRoom.MaPhong,
                RoomName = _selectedRoom.TenPhong,
                Capacity = 1   // default; user can edit in the right panel TextBox
            });
        }

        private bool CanExecuteSave()
            => SelectedRoom != null && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);

        private void ExecuteSave()
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 1. Save customer
                _context.KhachHangs.Add(NewCustomer);
                _context.SaveChanges();

                // 2. Create booking header
                var booking = new Model.DatPhong
                {
                    MaKhachHang = NewCustomer.MaKhachHang,
                    NgayDat = DateTime.Now,
                    TrangThaiDat = 1,
                    MaNhanVien = 1, // TODO: replace with LoginSession.CurrentUserId
                    TienCoc = 0
                };
                _context.DatPhongs.Add(booking);
                _context.SaveChanges();

                // 3. Create detail rows for all selected rooms
                foreach (var roomItem in SelectedRoomsList)
                {
                    var phong = AvailableRooms.First(r => r.MaPhong == roomItem.MaPhong);
                    var detail = new QuanLyKhachSan_SE104.Model.ChiTietDatPhong
                    {
                        MaDatPhong = booking.MaDatPhong,
                        MaPhong = roomItem.MaPhong,
                        NgayCheckIn = NgayCheckIn,
                        NgayCheckOut = NgayCheckOut,
                        GiaDat = phong.LoaiPhong.GiaMacDinh,
                        SoNguoi = roomItem.Capacity
                    };
                    _context.ChiTietDatPhongs.Add(detail);
                }

                _context.SaveChanges();
                transaction.Commit();

                MessageBox.Show($"Đặt phòng {SelectedRoom.TenPhong} thành công!", "Thông báo");
                ResetForm();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show("Lỗi khi lưu: " + ex.Message);
            }
        }

        private void ResetForm()
        {
            NewCustomer = new KhachHang();
            AvailableRooms.Clear();
            SelectedRoomsList.Clear();
            SelectedRoom = null;
        }
    }
}
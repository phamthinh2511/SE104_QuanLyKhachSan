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

        // ── SelectedRoom ─────────────────────────────────────────
        // Never set this to null inside SyncSelectedRoomsList.
        // CanExecuteSave depends on SelectedRoomsList.Count, not this property.
        // Single-click adds the room to the right panel list.
        private Phong _selectedRoom;
        public Phong SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom == value) return;
                _selectedRoom = value;
                OnPropertyChanged();
                if (_selectedRoom != null)
                    AddRoomToList(_selectedRoom);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Selected rooms list (right panel) ────────────────────
        private ObservableCollection<SelectedRoomItem> _selectedRoomsList = new();
        public ObservableCollection<SelectedRoomItem> SelectedRoomsList
        {
            get => _selectedRoomsList;
            set { _selectedRoomsList = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────
        public ICommand SearchRoomsCommand { get; }
        public ICommand SaveCommand { get; }

        // Dedicated command for double-click toggle.
        // Receives the card's Phong as CommandParameter so it always
        // knows exactly which room was double-clicked — no confusion
        // between room A and room B.
        public ICommand ToggleRoomCommand { get; }

        public DatPhongViewModel()
        {
            _context = new QuanLyKhachSanContext();

            ListGioiTinh = new ObservableCollection<string> { "Nam", "Nữ" };
            LoadInitialData();

            SearchRoomsCommand = new RelayCommand(ExecuteSearchRooms);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            ToggleRoomCommand = new RelayCommand<Phong>(ExecuteToggleRoom);
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
                SelectedRoomsList.Clear();
                _selectedRoom = null;
                OnPropertyChanged(nameof(SelectedRoom));

                foreach (var r in result)
                    AvailableRooms.Add(r);

                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn: " + ex.Message);
            }
        }

        // Single-click: add to right panel only if not already there.
        private void AddRoomToList(Phong phong)
        {
            if (!SelectedRoomsList.Any(x => x.MaPhong == phong.MaPhong))
            {
                SelectedRoomsList.Add(new SelectedRoomItem
                {
                    MaPhong = phong.MaPhong,
                    RoomName = phong.TenPhong,
                    Capacity = 1
                });
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Double-click: remove if present, add if not.
        // CommandParameter = the Phong bound to the card → always the correct room.
        private void ExecuteToggleRoom(Phong phong)
        {
            if (phong == null) return;

            var existing = SelectedRoomsList.FirstOrDefault(x => x.MaPhong == phong.MaPhong);
            if (existing != null)
            {
                SelectedRoomsList.Remove(existing);

                // Clear ListBox selection if we just removed the selected card
                if (_selectedRoom?.MaPhong == phong.MaPhong)
                {
                    _selectedRoom = null;
                    OnPropertyChanged(nameof(SelectedRoom));
                }
            }
            else
            {
                AddRoomToList(phong);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        // Check SelectedRoomsList.Count instead of SelectedRoom != null.
        // SelectedRoom can legitimately be null (e.g. after a search reset) even
        // when rooms are in the right panel — using Count is the correct guard.
        private bool CanExecuteSave()
            => SelectedRoomsList.Count > 0 && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);

        private void ExecuteSave()
        {
            foreach (var item in SelectedRoomsList)
            {
                var phong = _context.Phongs.Include(p => p.LoaiPhong)
                                    .FirstOrDefault(p => p.MaPhong == item.MaPhong);
                if (phong != null && item.Capacity > phong.LoaiPhong.SoNguoiToiDa)
                {
                    MessageBox.Show(
                        $"Phòng {item.RoomName} chỉ cho phép tối đa {phong.LoaiPhong.SoNguoiToiDa} người. Vui lòng chỉnh lại!",
                        "Lỗi nhập liệu");
                    return;
                }
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                _context.KhachHangs.Add(NewCustomer);
                _context.SaveChanges();

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

                foreach (var roomItem in SelectedRoomsList)
                {
                    var phong = AvailableRooms.First(r => r.MaPhong == roomItem.MaPhong);
                    _context.ChiTietDatPhongs.Add(new QuanLyKhachSan_SE104.Model.ChiTietDatPhong
                    {
                        MaDatPhong = booking.MaDatPhong,
                        MaPhong = roomItem.MaPhong,
                        NgayCheckIn = NgayCheckIn,
                        NgayCheckOut = NgayCheckOut,
                        GiaDat = phong.LoaiPhong.GiaMacDinh,
                        SoNguoi = roomItem.Capacity
                    });
                }

                _context.SaveChanges();
                transaction.Commit();

                var roomNames = string.Join(", ", SelectedRoomsList.Select(r => r.RoomName));
                MessageBox.Show($"Đặt phòng thành công: {roomNames}", "Thông báo");
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
            _selectedRoom = null;
            OnPropertyChanged(nameof(SelectedRoom));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
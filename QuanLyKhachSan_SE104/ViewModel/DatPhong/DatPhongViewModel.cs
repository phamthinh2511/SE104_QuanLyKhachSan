// ── Add this enum above the class ────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

public enum DatPhongMode
{
    Normal,       // Đặt phòng mới bình thường
    WalkIn,       // Khách lẻ: customer blank, room pre-selected, room grid locked
    DoiPhong      // Đổi phòng: customer pre-filled, pick a DIFFERENT room
}

public class DatPhongViewModel : INotifyPropertyChanged
{
    // ── NEW: mode + context objects ───────────────────────────────────────────
    public DatPhongMode Mode { get; private set; } = DatPhongMode.Normal;

    // For DoiPhong: the ChiTietDatPhong being transferred
    private ChiTietDatPhong _chiTietDatPhong;

    // Visibility helpers for the View
    public bool IsRoomGridVisible => Mode != DatPhongMode.WalkIn;
    public bool IsCustomerReadOnly => Mode == DatPhongMode.DoiPhong;

    // ── Existing fields (unchanged) ───────────────────────────────────────────
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly QuanLyKhachSanContext _context;

    private KhachHang _newCustomer = new KhachHang();
    public KhachHang NewCustomer
    {
        get => _newCustomer;
        set { _newCustomer = value; OnPropertyChanged(); }
    }

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

    private ObservableCollection<Phong> _availableRooms = new();
    public ObservableCollection<Phong> AvailableRooms
    {
        get => _availableRooms;
        set { _availableRooms = value; OnPropertyChanged(); }
    }

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

    private ObservableCollection<SelectedRoomItem> _selectedRoomsList = new();
    public ObservableCollection<SelectedRoomItem> SelectedRoomsList
    {
        get => _selectedRoomsList;
        set { _selectedRoomsList = value; OnPropertyChanged(); }
    }

    public ICommand SearchRoomsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ToggleRoomCommand { get; }
    public Action CloseAction { get; set; }

    // ── Constructor 1: Normal mode (existing behaviour) ───────────────────────
    public DatPhongViewModel()
    {
        _context = new QuanLyKhachSanContext();
        ListGioiTinh = new ObservableCollection<string> { "Nam", "Nữ" };
        LoadInitialData();

        SearchRoomsCommand = new RelayCommand(ExecuteSearchRooms);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ToggleRoomCommand = new RelayCommand<Phong>(ExecuteToggleRoom);
    }

    // ── Constructor 2: WalkIn — customer blank, room grid hidden ─────────────
    public DatPhongViewModel(Phong phong) : this()
    {
        Mode = DatPhongMode.WalkIn;
        OnPropertyChanged(nameof(IsRoomGridVisible));
        OnPropertyChanged(nameof(IsCustomerReadOnly));

        // Pre-select the room straight into the right-panel list
        AddRoomToList(phong);

        // Dates: check-in now, check-out tomorrow
        NgayCheckIn = DateTime.Today;
        NgayCheckOut = DateTime.Today.AddDays(1);
    }

    // ── Constructor 3: DoiPhong — customer pre-filled, pick a new room ────────
    public DatPhongViewModel(Phong currentPhong, ChiTietDatPhong chiTiet) : this()
    {
        Mode = DatPhongMode.DoiPhong;
        _chiTietDatPhong = chiTiet;
        OnPropertyChanged(nameof(IsRoomGridVisible));
        OnPropertyChanged(nameof(IsCustomerReadOnly));

        // Pre-fill customer
        if (chiTiet?.DatPhong?.KhachHang != null)
            NewCustomer = chiTiet.DatPhong.KhachHang;

        // Keep original dates
        NgayCheckIn = chiTiet?.NgayCheckIn ?? DateTime.Today;
        NgayCheckOut = chiTiet?.NgayCheckOut ?? DateTime.Today.AddDays(1);

        // Show all rooms except the current one so staff picks a DIFFERENT room
        ExecuteSearchRoomsExcluding(currentPhong.MaPhong);
    }

    // ── Data loading ──────────────────────────────────────────────────────────
    private void LoadInitialData()
    {
        try
        {
            ListTang = new ObservableCollection<int>(
                _context.Phongs.Select(p => p.SoTang).Distinct().OrderBy(t => t).ToList());

            ListLoaiPhong = new ObservableCollection<LoaiPhong>(
                _context.LoaiPhongs.ToList());

            OnPropertyChanged(nameof(ListTang));
            OnPropertyChanged(nameof(ListLoaiPhong));
        }
        catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
    }

    // ── Search (normal) ───────────────────────────────────────────────────────
    private void ExecuteSearchRooms()
    {
        try
        {
            var busyIds = GetBusyRoomIds();
            var query = BuildRoomQuery(busyIds, excludeMaPhong: null);

            AvailableRooms.Clear();
            SelectedRoomsList.Clear();
            _selectedRoom = null;
            OnPropertyChanged(nameof(SelectedRoom));

            foreach (var r in query.ToList()) AvailableRooms.Add(r);
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi truy vấn: " + ex.Message); }
    }

    // ── Search for DoiPhong — excludes the room being vacated ────────────────
    private void ExecuteSearchRoomsExcluding(int excludeMaPhong)
    {
        try
        {
            var busyIds = GetBusyRoomIds();
            var query = BuildRoomQuery(busyIds, excludeMaPhong);

            AvailableRooms.Clear();
            foreach (var r in query.ToList()) AvailableRooms.Add(r);
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi truy vấn: " + ex.Message); }
    }

    private List<int> GetBusyRoomIds() =>
        _context.ChiTietDatPhongs
            .Where(ct => !(NgayCheckOut <= ct.NgayCheckIn || NgayCheckIn >= ct.NgayCheckOut))
            .Select(ct => ct.MaPhong)
            .ToList();

    private IQueryable<Phong> BuildRoomQuery(List<int> busyIds, int? excludeMaPhong)
    {
        var q = _context.Phongs
            .Include(r => r.LoaiPhong)
            .Where(r => !busyIds.Contains(r.MaPhong) && r.TrangThai == 0)
            .AsQueryable();

        if (excludeMaPhong.HasValue)
            q = q.Where(r => r.MaPhong != excludeMaPhong.Value);

        if (SelectedTang > 0)
            q = q.Where(r => r.SoTang == SelectedTang);

        if (SelectedLoaiPhong != null)
            q = q.Where(r => r.MaLoaiPhong == SelectedLoaiPhong.MaLoaiPhong);

        return q;
    }

    // ── Room list helpers ─────────────────────────────────────────────────────
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

    private void ExecuteToggleRoom(Phong phong)
    {
        if (phong == null) return;
        var existing = SelectedRoomsList.FirstOrDefault(x => x.MaPhong == phong.MaPhong);
        if (existing != null)
        {
            SelectedRoomsList.Remove(existing);
            if (_selectedRoom?.MaPhong == phong.MaPhong)
            {
                _selectedRoom = null;
                OnPropertyChanged(nameof(SelectedRoom));
            }
        }
        else AddRoomToList(phong);

        CommandManager.InvalidateRequerySuggested();
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    private bool CanExecuteSave()
        => SelectedRoomsList.Count > 0 && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);

    private void ExecuteSave()
    {
        // Validate capacity
        foreach (var item in SelectedRoomsList)
        {
            var phong = _context.Phongs.Include(p => p.LoaiPhong)
                                .FirstOrDefault(p => p.MaPhong == item.MaPhong);
            if (phong != null && item.Capacity > phong.LoaiPhong.SoNguoiToiDa)
            {
                MessageBox.Show(
                    $"Phòng {item.RoomName} chỉ cho phép tối đa {phong.LoaiPhong.SoNguoiToiDa} người.",
                    "Lỗi nhập liệu");
                return;
            }
        }

        if (Mode == DatPhongMode.DoiPhong)
            ExecuteDoiPhongSave();
        else
            ExecuteNormalSave();
    }

    // ── Save: Normal / WalkIn ─────────────────────────────────────────────────
    private void ExecuteNormalSave()
    {
        using var tx = _context.Database.BeginTransaction();
        try
        {
            // For WalkIn, customer is new. For Normal, also new.
            _context.KhachHangs.Add(NewCustomer);
            _context.SaveChanges();

            var trangThaiDat = Mode == DatPhongMode.WalkIn ? 2 : 1; // WalkIn = đang ở
            var booking = new DatPhong
            {
                MaKhachHang = NewCustomer.MaKhachHang,
                NgayDat = DateTime.Now,
                TrangThaiDat = trangThaiDat,
                MaNhanVien = 1, // TODO: LoginSession.CurrentUserId
                TienCoc = 0
            };
            _context.DatPhongs.Add(booking);
            _context.SaveChanges();

            foreach (var roomItem in SelectedRoomsList)
            {
                var phongDb = _context.Phongs.Include(p => p.LoaiPhong)
                                      .First(p => p.MaPhong == roomItem.MaPhong);

                phongDb.TrangThai = Mode == DatPhongMode.WalkIn ? 2 : 1;

                _context.ChiTietDatPhongs.Add(new ChiTietDatPhong
                {
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = roomItem.MaPhong,
                    NgayCheckIn = NgayCheckIn,
                    NgayCheckOut = NgayCheckOut,
                    GiaDat = phongDb.LoaiPhong.GiaMacDinh,
                    SoNguoi = roomItem.Capacity
                });
            }

            _context.SaveChanges();
            tx.Commit();

            var label = Mode == DatPhongMode.WalkIn ? "Check-in khách lẻ" : "Đặt phòng";
            MessageBox.Show($"{label} thành công: " +
                string.Join(", ", SelectedRoomsList.Select(r => r.RoomName)), "Thông báo");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi lưu: " + ex.Message); }
    }

    // ── Save: DoiPhong ────────────────────────────────────────────────────────
    private void ExecuteDoiPhongSave()
    {
        if (_chiTietDatPhong == null || SelectedRoomsList.Count == 0) return;
        var newRoomItem = SelectedRoomsList.First();

        using var tx = _context.Database.BeginTransaction();
        try
        {
            // 1. Free the old room
            var oldPhong = _context.Phongs.Find(_chiTietDatPhong.MaPhong);
            if (oldPhong != null) oldPhong.TrangThai = 0;

            // 2. Occupy the new room
            var newPhong = _context.Phongs.Include(p => p.LoaiPhong)
                                   .First(p => p.MaPhong == newRoomItem.MaPhong);
            newPhong.TrangThai = _chiTietDatPhong.DatPhong?.TrangThaiDat == 2 ? 2 : 1;

            // 3. Re-point ChiTietDatPhong → new room, update price
            var ct = _context.ChiTietDatPhongs.Find(_chiTietDatPhong.MaChiTietDatPhong);
            if (ct != null)
            {
                ct.MaPhong = newRoomItem.MaPhong;
                ct.GiaDat = newPhong.LoaiPhong.GiaMacDinh;
                ct.NgayCheckIn = NgayCheckIn;
                ct.NgayCheckOut = NgayCheckOut;
                ct.SoNguoi = newRoomItem.Capacity;
                // ChiTietDichVus stay linked via MaChiTietDatPhong — no change needed
            }

            _context.SaveChanges();
            tx.Commit();

            MessageBox.Show($"Đổi sang phòng {newPhong.TenPhong} thành công!", "Thông báo");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi đổi phòng: " + ex.Message); }
    }
}
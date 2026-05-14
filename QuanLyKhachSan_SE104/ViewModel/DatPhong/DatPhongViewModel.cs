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
    DoiPhong,     // Đổi phòng: customer pre-filled, pick a DIFFERENT room
    GiaHan        // Gia hạn: chỉ cho sửa NgayCheckOut, khóa mọi thứ khác
}

public class DatPhongViewModel : INotifyPropertyChanged
{
    // ── mode + context objects ───────────────────────────────────────────
    public DatPhongMode Mode { get; private set; } = DatPhongMode.Normal;

    // For DoiPhong / GiaHan: the ChiTietDatPhong being modified
    private ChiTietDatPhong _chiTietDatPhong;

    // Visibility helpers for the View
    public bool IsRoomGridVisible => Mode != DatPhongMode.WalkIn && Mode != DatPhongMode.GiaHan;
    public bool IsCustomerReadOnly => Mode == DatPhongMode.DoiPhong || Mode == DatPhongMode.GiaHan;

    // GiaHan mode — khoá luôn cả ngày check-in và selector tầng/loại phòng
    public bool IsCheckInReadOnly => Mode == DatPhongMode.GiaHan;
    public bool IsFilterVisible => Mode != DatPhongMode.GiaHan;

    // ── Existing fields ───────────────────────────────────────────────────
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

    private DateTime _ngayCheckIn = DateTime.Now;
    public DateTime NgayCheckIn
    {
        get => _ngayCheckIn;
        set { _ngayCheckIn = value; OnPropertyChanged(); }
    }

    private DateTime _ngayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);
    public DateTime NgayCheckOut
    {
        get => _ngayCheckOut;
        set
        {
            DateTime dateWithNoon = value.Date.AddHours(12);

            if (dateWithNoon <= NgayCheckIn)
            {
                MessageBox.Show("Ngày check-out phải sau thời điểm check-in.", "Lỗi nhập liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                OnPropertyChanged();
                return;
            }
            _ngayCheckOut = dateWithNoon;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> ListGioiTinh { get; set; }
    public ObservableCollection<string> ListQuocTich { get; set; }
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

            // KIỂM TRA TRẠNG THÁI DỌN DẸP
            if (value != null && value.TrangThaiDonDep == 1)
            {
                var result = MessageBox.Show(
                    $"Phòng {value.TenPhong} hiện đang trong quá trình dọn dẹp.\nBạn đã xác nhận với nhân viên vệ sinh rằng phòng đã sẵn sàng phục vụ chưa?",
                    "Xác nhận sẵn sàng",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Nếu User bấm Yes, cập nhật trạng thái phòng thành "Sạch" (0)
                    UpdateRoomCleaningStatus(value);
                }
                else
                {
                    // Nếu User bấm No, không chọn phòng này nữa
                    OnPropertyChanged();
                    return;
                }
            }

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

    // ── Constructor 1: Normal mode ────────────────────────────────────────
    public DatPhongViewModel()
    {
        _context = new QuanLyKhachSanContext();
        ListGioiTinh = new ObservableCollection<string> { "Nam", "Nữ" };
        ListQuocTich = new ObservableCollection<string> { "Việt Nam", "Nước Ngoài" };
        LoadInitialData();

        SearchRoomsCommand = new RelayCommand(ExecuteSearchRooms);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ToggleRoomCommand = new RelayCommand<Phong>(ExecuteToggleRoom);
    }

    // ── Constructor 2: WalkIn ─────────────────────────────────────────────
    public DatPhongViewModel(Phong phong) : this()
    {
        Mode = DatPhongMode.WalkIn;
        NotifyModeProps();

        AddRoomToList(phong);

        NgayCheckIn = DateTime.Now;
        NgayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);
    }

    // ── Constructor 3: DoiPhong ───────────────────────────────────────────
    public DatPhongViewModel(Phong currentPhong, ChiTietDatPhong chiTiet) : this()
    {
        Mode = DatPhongMode.DoiPhong;
        _chiTietDatPhong = chiTiet;
        NotifyModeProps();

        if (chiTiet?.DatPhong?.KhachHang != null)
            NewCustomer = chiTiet.DatPhong.KhachHang;

        NgayCheckIn = chiTiet?.NgayCheckIn ?? DateTime.Now;
        NgayCheckOut = chiTiet?.NgayCheckOut ?? DateTime.Today.AddDays(1).Date.AddHours(12);

        ExecuteSearchRoomsExcluding(currentPhong.MaPhong);
    }

    // ── Constructor 4: GiaHan ─────────────────────────────────────────────
    /// <summary>
    /// Gia hạn phòng quá hạn: chỉ cho phép thay đổi NgayCheckOut.
    /// NgayCheckOut mặc định = hôm nay + 1 ngày (nhân viên tự chỉnh).
    /// </summary>
    public DatPhongViewModel(Phong currentPhong, ChiTietDatPhong chiTiet, bool giaHan) : this()
    {
        Mode = DatPhongMode.GiaHan;
        _chiTietDatPhong = chiTiet;
        NotifyModeProps();

        // Điền thông tin khách (read-only)
        if (chiTiet?.DatPhong?.KhachHang != null)
            NewCustomer = chiTiet.DatPhong.KhachHang;

        // Giữ nguyên check-in thực tế
        NgayCheckIn = chiTiet?.NgayCheckIn ?? DateTime.Now;

        // Check-out mặc định = ngày hôm nay + 1 (nhân viên điều chỉnh)
        NgayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);

        // Hiển thị phòng hiện tại trong danh sách (locked, không cho chọn thêm)
        AddRoomToList(currentPhong);
    }

    // ── Data loading ──────────────────────────────────────────────────────
    private void LoadInitialData()
    {
        try
        {
            ListTang = new ObservableCollection<int>(
                _context.Phongs
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.SoTang).Distinct().OrderBy(t => t).ToList());

            ListLoaiPhong = new ObservableCollection<LoaiPhong>(
                _context.LoaiPhongs.Where(lp => !lp.IsDeleted).ToList());

            OnPropertyChanged(nameof(ListTang));
            OnPropertyChanged(nameof(ListLoaiPhong));
        }
        catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
    }

    // ── Search (normal) ───────────────────────────────────────────────────
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

    // ── Search for DoiPhong ───────────────────────────────────────────────
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
            .Where(r => !busyIds.Contains(r.MaPhong) && r.TrangThai == 0 && !r.IsDeleted)
            .AsQueryable();

        if (excludeMaPhong.HasValue)
            q = q.Where(r => r.MaPhong != excludeMaPhong.Value);

        if (SelectedTang > 0)
            q = q.Where(r => r.SoTang == SelectedTang);

        if (SelectedLoaiPhong != null)
            q = q.Where(r => r.MaLoaiPhong == SelectedLoaiPhong.MaLoaiPhong);

        return q;
    }

    // ── Room list helpers ─────────────────────────────────────────────────
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
        // Không cho xoá phòng trong mode GiaHan
        if (Mode == DatPhongMode.GiaHan) return;

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

    // ── Save ──────────────────────────────────────────────────────────────
    private bool CanExecuteSave()
        => SelectedRoomsList.Count > 0 && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);

    private void ExecuteSave()
    {
        // Validate capacity (skip for GiaHan — room/capacity unchanged)
        if (Mode != DatPhongMode.GiaHan)
        {
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
        }

        switch (Mode)
        {
            case DatPhongMode.DoiPhong: ExecuteDoiPhongSave(); break;
            case DatPhongMode.GiaHan: ExecuteGiaHanSave(); break;
            default: ExecuteNormalSave(); break;
        }
    }

    // ── Save: Normal / WalkIn ─────────────────────────────────────────────
    private void ExecuteNormalSave()
    {
        using var tx = _context.Database.BeginTransaction();
        try
        {
            _context.KhachHangs.Add(NewCustomer);
            _context.SaveChanges();

            var trangThaiDat = Mode == DatPhongMode.WalkIn ? 2 : 1;
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

            if (Mode == DatPhongMode.Normal)
                ResetFields();
            else
                CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi lưu: " + ex.Message); }
    }

    // ── Save: DoiPhong ────────────────────────────────────────────────────
    private void ExecuteDoiPhongSave()
    {
        if (_chiTietDatPhong == null || SelectedRoomsList.Count == 0) return;
        var newRoomItem = SelectedRoomsList.First();

        using var tx = _context.Database.BeginTransaction();
        try
        {
            var oldPhong = _context.Phongs.Find(_chiTietDatPhong.MaPhong);
            if (oldPhong != null) oldPhong.TrangThai = 0;

            var newPhong = _context.Phongs.Include(p => p.LoaiPhong)
                                   .First(p => p.MaPhong == newRoomItem.MaPhong);
            newPhong.TrangThai = _chiTietDatPhong.DatPhong?.TrangThaiDat == 2 ? 2 : 1;

            var ct = _context.ChiTietDatPhongs.Find(_chiTietDatPhong.MaChiTietDatPhong);
            if (ct != null)
            {
                ct.MaPhong = newRoomItem.MaPhong;
                ct.GiaDat = newPhong.LoaiPhong.GiaMacDinh;
                ct.NgayCheckIn = NgayCheckIn;
                ct.NgayCheckOut = NgayCheckOut;
                ct.SoNguoi = newRoomItem.Capacity;
            }

            _context.SaveChanges();
            tx.Commit();

            MessageBox.Show($"Đổi sang phòng {newPhong.TenPhong} thành công!", "Thông báo");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi đổi phòng: " + ex.Message); }
    }

    // ── Save: GiaHan ──────────────────────────────────────────────────────
    /// <summary>
    /// Chỉ cập nhật NgayCheckOut mới trên ChiTietDatPhong.
    /// Trạng thái phòng giữ nguyên (vẫn là 3-Quá hạn cho đến khi LoadData chạy lại;
    /// vì NgayCheckOut mới > hôm nay nên lần LoadData kế tiếp sẽ bỏ flag quá hạn).
    /// </summary>
    private void ExecuteGiaHanSave()
    {
        if (_chiTietDatPhong == null) return;

        using var tx = _context.Database.BeginTransaction();
        try
        {
            var ct = _context.ChiTietDatPhongs.Find(_chiTietDatPhong.MaChiTietDatPhong);
            if (ct == null) { MessageBox.Show("Không tìm thấy thông tin đặt phòng."); return; }

            ct.NgayCheckOut = NgayCheckOut;

            // Reset trạng thái phòng về Đang ở (2) vì đã gia hạn hợp lệ
            var phong = _context.Phongs.Find(ct.MaPhong);
            if (phong != null) phong.TrangThai = 2;

            // Đảm bảo DatPhong vẫn là Đã nhận phòng (2)
            var dat = _context.DatPhongs.Find(ct.MaDatPhong);
            if (dat != null && dat.TrangThaiDat != 2) dat.TrangThaiDat = 2;

            _context.SaveChanges();
            tx.Commit();

            MessageBox.Show($"Gia hạn phòng đến {NgayCheckOut:dd/MM/yyyy HH:mm} thành công!", "Thông báo");
            CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi gia hạn: " + ex.Message); }
    }

    private void UpdateRoomCleaningStatus(Phong phong)
    {
        try
        {
            // Tìm phòng trong context và cập nhật
            var phongDb = _context.Phongs.Find(phong.MaPhong);
            if (phongDb != null)
            {
                phongDb.TrangThaiDonDep = 0; // Chuyển về Sạch
                _context.SaveChanges();

                // Cập nhật lại UI của đối tượng đang chọn
                phong.TrangThaiDonDep = 0;
                OnPropertyChanged(nameof(AvailableRooms));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi cập nhật trạng thái dọn dẹp: " + ex.Message);
        }
    }

    private void ResetFields()
    {
        NewCustomer = new KhachHang();
        NgayCheckIn = DateTime.Now;
        NgayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);
        SelectedRoomsList.Clear();
        SelectedRoom = null;
        SelectedTang = 0;
        SelectedLoaiPhong = null;
        AvailableRooms.Clear();
        CommandManager.InvalidateRequerySuggested();
    }

    private void NotifyModeProps()
    {
        OnPropertyChanged(nameof(IsRoomGridVisible));
        OnPropertyChanged(nameof(IsCustomerReadOnly));
        OnPropertyChanged(nameof(IsCheckInReadOnly));
        OnPropertyChanged(nameof(IsFilterVisible));
    }
}
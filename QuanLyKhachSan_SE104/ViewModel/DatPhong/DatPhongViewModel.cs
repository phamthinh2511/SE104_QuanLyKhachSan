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
    // ── Mode + context objects ────────────────────────────────────────────
    public DatPhongMode Mode { get; private set; } = DatPhongMode.Normal;
    private ChiTietDatPhong _chiTietDatPhong;

    // Visibility / lock helpers for the View
    public bool IsRoomGridVisible => Mode != DatPhongMode.WalkIn && Mode != DatPhongMode.GiaHan;
    public bool IsCustomerReadOnly => Mode == DatPhongMode.DoiPhong || Mode == DatPhongMode.GiaHan;
    public bool IsCheckInReadOnly => Mode == DatPhongMode.GiaHan;
    public bool IsFilterVisible => Mode != DatPhongMode.GiaHan;

    // Deposit field is read-only in DoiPhong (inherits existing deposit) and GiaHan
    public bool IsTienCocReadOnly => Mode == DatPhongMode.DoiPhong || Mode == DatPhongMode.GiaHan;

    // ── INotifyPropertyChanged ────────────────────────────────────────────
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly QuanLyKhachSanContext _context;

    // ── Customer ──────────────────────────────────────────────────────────
    private KhachHang _newCustomer = new KhachHang();
    public KhachHang NewCustomer
    {
        get => _newCustomer;
        set { _newCustomer = value; OnPropertyChanged(); }
    }

    // ── Dates ─────────────────────────────────────────────────────────────
    private DateTime _ngayCheckIn = DateTime.Now;
    public DateTime NgayCheckIn
    {
        get => _ngayCheckIn;
        set
        {
            _ngayCheckIn = value;
            OnPropertyChanged();
            RecalculateDefaultDeposit();   // default deposit changes when dates change
        }
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
            RecalculateDefaultDeposit();   // default deposit changes when dates change
        }
    }

    // ── Deposit ───────────────────────────────────────────────────────────

    /// <summary>
    /// Minimum deposit = 1st-night rate of the cheapest selected room.
    /// Recalculated whenever dates or selected rooms change.
    /// Displayed as a hint below the TienCoc TextBox.
    /// </summary>
    private decimal _minTienCoc = 0;
    public decimal MinTienCoc
    {
        get => _minTienCoc;
        private set { _minTienCoc = value; OnPropertyChanged(); OnPropertyChanged(nameof(MinTienCocHint)); }
    }

    public string MinTienCocHint =>
        MinTienCoc > 0
            ? $"Tối thiểu: {MinTienCoc:#,0}₫ (1 đêm)"
            : "Chọn phòng để tính tiền cọc tối thiểu";

    private decimal _tienCoc = 0;
    public decimal TienCoc
    {
        get => _tienCoc;
        set
        {
            _tienCoc = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TienCocText));
        }
    }

    /// <summary>Two-way text binding for the deposit TextBox (formats with commas).</summary>
    public string TienCocText
    {
        get => _tienCoc == 0 ? "" : _tienCoc.ToString("N0");
        set
        {
            // Strip formatting characters before parsing
            string clean = (value ?? "").Replace(",", "").Replace(".", "").Trim();
            if (decimal.TryParse(clean, out var parsed))
                TienCoc = parsed;
            else if (string.IsNullOrEmpty(clean))
                TienCoc = 0;
            // Invalid input: ignore, keep current value
        }
    }

    // ── Filter combos ─────────────────────────────────────────────────────
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

    // ── Room lists ────────────────────────────────────────────────────────
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

            if (value != null && value.TrangThaiDonDep == 1)
            {
                var result = MessageBox.Show(
                    $"Phòng {value.TenPhong} đang trong quá trình dọn dẹp.\n" +
                    "Bạn đã xác nhận phòng sẵn sàng phục vụ chưa?",
                    "Xác nhận sẵn sàng",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    UpdateRoomCleaningStatus(value);
                else
                {
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

    // ── Commands ──────────────────────────────────────────────────────────
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
        // Deposit = 0 for walk-in (paid immediately at checkout)
        TienCoc = 0;
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

        // Carry over existing deposit (Rule 04)
        TienCoc = chiTiet?.DatPhong?.TienCoc ?? 0;

        ExecuteSearchRoomsExcluding(currentPhong.MaPhong);
    }

    // ── Constructor 4: GiaHan ─────────────────────────────────────────────
    public DatPhongViewModel(Phong currentPhong, ChiTietDatPhong chiTiet, bool giaHan) : this()
    {
        Mode = DatPhongMode.GiaHan;
        _chiTietDatPhong = chiTiet;
        NotifyModeProps();

        if (chiTiet?.DatPhong?.KhachHang != null)
            NewCustomer = chiTiet.DatPhong.KhachHang;

        NgayCheckIn = chiTiet?.NgayCheckIn ?? DateTime.Now;
        NgayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);

        // Carry over existing deposit (read-only in GiaHan mode)
        TienCoc = chiTiet?.DatPhong?.TienCoc ?? 0;

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

    private void RecalculateDefaultDeposit()
    {
        if (SelectedRoomsList == null || SelectedRoomsList.Count == 0)
        {
            MinTienCoc = 0;
            return;
        }

        // Cheapest room's nightly rate drives the minimum
        var maPhongList = SelectedRoomsList.Select(r => r.MaPhong).ToList();
        var lowestRate = _context.Phongs
            .Include(p => p.LoaiPhong)
            .Where(p => maPhongList.Contains(p.MaPhong))
            .Select(p => p.LoaiPhong.GiaMacDinh)
            .AsEnumerable()
            .DefaultIfEmpty(0)
            .Min();

        MinTienCoc = lowestRate;

        // Auto-fill only if the user hasn't entered anything yet
        if (TienCoc < MinTienCoc)
            TienCoc = MinTienCoc;
    }

    // ── Search ────────────────────────────────────────────────────────────
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
            .Where(ct =>
                // Only active bookings (confirmed=1 or checked-in=2) block a room
                (ct.DatPhong.TrangThaiDat == 1 || ct.DatPhong.TrangThaiDat == 2) &&
                // Date range overlaps with the requested period
                !(NgayCheckOut <= ct.NgayCheckIn || NgayCheckIn >= ct.NgayCheckOut))
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
            RecalculateDefaultDeposit();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ExecuteToggleRoom(Phong phong)
    {
        if (phong == null) return;
        if (Mode == DatPhongMode.GiaHan) return;   // locked in GiaHan

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

        RecalculateDefaultDeposit();
        CommandManager.InvalidateRequerySuggested();
    }

    // ── Save ──────────────────────────────────────────────────────────────
    private bool CanExecuteSave()
        => SelectedRoomsList.Count > 0 && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);

    private void ExecuteSave()
    {
        // Validate deposit minimum (skip for GiaHan and DoiPhong)
        if (Mode == DatPhongMode.Normal && TienCoc < MinTienCoc)
        {
            MessageBox.Show(
                $"Tiền cọc tối thiểu là {MinTienCoc:#,0}₫ (bằng giá 1 đêm).",
                "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate capacity (skip for GiaHan)
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
            var depositAmount = Mode == DatPhongMode.WalkIn ? 0 : TienCoc;

            var booking = new DatPhong
            {
                MaKhachHang = NewCustomer.MaKhachHang,
                NgayDat = DateTime.Now,
                TrangThaiDat = trangThaiDat,
                MaNhanVien = LoginSession.CurrentNhanVienId,
                TienCoc = depositAmount,
                TrangThaiCoc = depositAmount > 0 ? 0 : 2  // 0=Đang giữ, 2=Không cọc(thu luôn)
            };
            _context.DatPhongs.Add(booking);
            _context.SaveChanges();

            // Write audit log entry if a deposit was collected
            if (depositAmount > 0)
            {
                _context.LichSuCocs.Add(new LichSuCoc
                {
                    MaDatPhong = booking.MaDatPhong,
                    LoaiGiaoDich = 0,              // Thu cọc
                    SoTien = depositAmount,
                    ThoiGian = DateTime.Now,
                    MaNhanVien = LoginSession.CurrentNhanVienId,
                    GhiChu = $"Thu cọc khi đặt phòng {string.Join(", ", SelectedRoomsList.Select(r => r.RoomName))}"
                });
            }

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
            MessageBox.Show(
                $"{label} thành công: {string.Join(", ", SelectedRoomsList.Select(r => r.RoomName))}\n" +
                (depositAmount > 0 ? $"Tiền cọc đã thu: {depositAmount:#,0}₫" : ""),
                "Thông báo");

            HotelEventBus.PublishRoomStatusChanged();

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
            DateTime thoiDiemDoiPhong = DateTime.Now;

            // 1. Giải phóng phòng cũ về trạng thái trống (Trạng thái = 0)
            var oldPhong = _context.Phongs.Find(_chiTietDatPhong.MaPhong);
            if (oldPhong != null) oldPhong.TrangThai = 0;

            // 2. Chốt thời gian ở và giữ nguyên giá của PHÒNG CŨ
            var ctOld = _context.ChiTietDatPhongs.Find(_chiTietDatPhong.MaChiTietDatPhong);
            if (ctOld != null)
            {
                // Ngày check-out thực tế của phòng cũ chính là lúc thực hiện đổi phòng
                ctOld.NgayCheckOut = thoiDiemDoiPhong;
            }

            // 3. Khởi tạo và thiết lập trạng thái cho PHÒNG MỚI
            var newPhong = _context.Phongs.Include(p => p.LoaiPhong)
                                          .First(p => p.MaPhong == newRoomItem.MaPhong);
            // Giữ nguyên trạng thái thuê của hóa đơn tổng (Đặt trước = 1 hoặc Khách lẻ = 2)
            newPhong.TrangThai = _chiTietDatPhong.DatPhong?.TrangThaiDat == 2 ? 2 : 1;

            // 4. THÊM MỚI một dòng chi tiết cho phòng mới (Không UPDATE dòng cũ nữa)
            _context.ChiTietDatPhongs.Add(new ChiTietDatPhong
            {
                MaDatPhong = _chiTietDatPhong.MaDatPhong,
                MaPhong = newRoomItem.MaPhong,
                NgayCheckIn = thoiDiemDoiPhong,               // Bắt đầu tính tiền phòng mới từ lúc này
                NgayCheckOut = NgayCheckOut,                  // Đến ngày dự kiến trả phòng ban đầu
                GiaDat = newPhong.LoaiPhong.GiaMacDinh,       // Đơn giá phòng mới (80k)
                SoNguoi = newRoomItem.Capacity
            });

            // 5. Ghi lịch sử chuyển cọc (Audit Log)
            var dat = _context.DatPhongs.Find(_chiTietDatPhong.MaDatPhong);
            if (dat != null && dat.TienCoc > 0)
            {
                _context.LichSuCocs.Add(new LichSuCoc
                {
                    MaDatPhong = dat.MaDatPhong,
                    LoaiGiaoDich = 3, // Chuyển booking
                    SoTien = dat.TienCoc,
                    ThoiGian = thoiDiemDoiPhong,
                    MaNhanVien = LoginSession.CurrentNhanVienId,
                    GhiChu = $"Đổi phòng: {oldPhong?.TenPhong} → {newPhong.TenPhong}. Cọc giữ nguyên.",
                    MaDatPhongMoi = dat.MaDatPhong
                });
            }

            _context.SaveChanges();
            tx.Commit();

            MessageBox.Show($"Đổi sang phòng {newPhong.TenPhong} thành công!", "Thông báo");
            HotelEventBus.PublishRoomStatusChanged();
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            MessageBox.Show("Lỗi đổi phòng: " + ex.Message);
        }
    }

    // ── Save: GiaHan ──────────────────────────────────────────────────────
    private void ExecuteGiaHanSave()
    {
        if (_chiTietDatPhong == null) return;

        using var tx = _context.Database.BeginTransaction();
        try
        {
            var ct = _context.ChiTietDatPhongs.Find(_chiTietDatPhong.MaChiTietDatPhong);
            if (ct == null) { MessageBox.Show("Không tìm thấy thông tin đặt phòng."); return; }

            ct.NgayCheckOut = NgayCheckOut;

            var phong = _context.Phongs.Find(ct.MaPhong);
            if (phong != null) phong.TrangThai = 2;

            var dat = _context.DatPhongs.Find(ct.MaDatPhong);
            if (dat != null && dat.TrangThaiDat != 2) dat.TrangThaiDat = 2;

            _context.SaveChanges();
            tx.Commit();

            MessageBox.Show($"Gia hạn phòng đến {NgayCheckOut:dd/MM/yyyy HH:mm} thành công!", "Thông báo");
            HotelEventBus.PublishRoomStatusChanged();
            CloseAction?.Invoke();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show("Lỗi gia hạn: " + ex.Message); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void UpdateRoomCleaningStatus(Phong phong)
    {
        try
        {
            var phongDb = _context.Phongs.Find(phong.MaPhong);
            if (phongDb != null)
            {
                phongDb.TrangThaiDonDep = 0;
                _context.SaveChanges();
                phong.TrangThaiDonDep = 0;
                OnPropertyChanged(nameof(AvailableRooms));
            }
        }
        catch (Exception ex) { MessageBox.Show("Lỗi cập nhật trạng thái dọn dẹp: " + ex.Message); }
    }

    private void ResetFields()
    {
        NewCustomer = new KhachHang();
        NgayCheckIn = DateTime.Now;
        NgayCheckOut = DateTime.Today.AddDays(1).Date.AddHours(12);
        TienCoc = 0;
        MinTienCoc = 0;
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
        OnPropertyChanged(nameof(IsTienCocReadOnly));
    }
}
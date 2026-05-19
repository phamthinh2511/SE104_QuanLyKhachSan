using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.DTO;
using QuanLyKhachSan_SE104.DTOs;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Services;
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

    private readonly RoomService _roomService = new();
    private readonly BookingService _bookingService = new();

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
        NgayCheckOut = chiTiet?.NgayCheckOut ?? DateTime.Today.AddDays(1).Date.AddHours(12);

        // Carry over existing deposit (read-only in GiaHan mode)
        TienCoc = chiTiet?.DatPhong?.TienCoc ?? 0;

        AddRoomToList(currentPhong);
    }

    // ── Data loading ──────────────────────────────────────────────────────
    private void LoadInitialData()
    {
        try
        {
            using var ctx = new QuanLyKhachSanContext();

            ListTang = new ObservableCollection<int>(
                ctx.Phongs
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.SoTang).Distinct().OrderBy(t => t).ToList());

            ListLoaiPhong = new ObservableCollection<LoaiPhong>(
                ctx.LoaiPhongs.Where(lp => !lp.IsDeleted).ToList());

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

        MinTienCoc = _roomService.TinhTienCocToiThieu(SelectedRoomsList.Select(r => r.MaPhong));

        // Auto-fill only if the user hasn't entered anything yet
        if (TienCoc < MinTienCoc && Mode == DatPhongMode.Normal)
            TienCoc = MinTienCoc;
    }

    // ── Search ────────────────────────────────────────────────────────────
    private void ExecuteSearchRooms()
    {
        try
        {
            var rooms = _roomService.TimPhongTrong(CreateRoomSearchDTO(), excludeMaPhong: null);

            AvailableRooms.Clear();
            SelectedRoomsList.Clear();
            _selectedRoom = null;
            OnPropertyChanged(nameof(SelectedRoom));

            foreach (var r in rooms) AvailableRooms.Add(ToPhongModel(r));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi truy vấn: " + ex.Message); }
    }

    private void ExecuteSearchRoomsExcluding(int excludeMaPhong)
    {
        try
        {
            var rooms = _roomService.TimPhongTrong(CreateRoomSearchDTO(), excludeMaPhong);

            AvailableRooms.Clear();
            foreach (var r in rooms) AvailableRooms.Add(ToPhongModel(r));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi truy vấn: " + ex.Message); }
    }

    private RoomSearchDTO CreateRoomSearchDTO()
    {
        return new RoomSearchDTO
        {
            NgayCheckIn = NgayCheckIn,
            NgayCheckOut = NgayCheckOut,
            SoTang = SelectedTang > 0 ? SelectedTang : null,
            MaLoaiPhong = SelectedLoaiPhong?.MaLoaiPhong > 0 ? SelectedLoaiPhong.MaLoaiPhong : null
        };
    }

    private static Phong ToPhongModel(PhongDTO dto)
    {
        return new Phong
        {
            MaPhong = dto.MaPhong,
            TenPhong = dto.TenPhong,
            SoTang = dto.SoTang,
            TrangThai = 0,
            TrangThaiDonDep = dto.TrangThaiDonDep,
            LoaiPhong = new LoaiPhong
            {
                TenLoaiPhong = dto.TenLoaiPhong,
                GiaMacDinh = dto.GiaMacDinh,
                SoNguoiToiDa = dto.SoNguoiToiDa
            }
        };
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

    // ── Save Router ────────────────────────────────────────────────────────
    private bool CanExecuteSave()
    {
        if (Mode == DatPhongMode.GiaHan)
            return _chiTietDatPhong != null;

        return SelectedRoomsList.Count > 0 && !string.IsNullOrWhiteSpace(NewCustomer?.HoTen);
    }

    private void ExecuteSave()
    {
        var validationMessage = ValidateBeforeSave();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            MessageBox.Show(validationMessage, "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BookingResult result = Mode switch
        {
            DatPhongMode.DoiPhong => _bookingService.DoiPhong(CreateDoiPhongRequest()),
            DatPhongMode.GiaHan => _bookingService.GiaHan(CreateGiaHanRequest()),
            _ => _bookingService.TaoDatPhong(CreateBookingRequest())
        };

        if (!result.IsSuccess)
        {
            var icon = result.IsConflict ? MessageBoxImage.Stop : MessageBoxImage.Warning;
            MessageBox.Show(result.Message, result.IsConflict ? "Lỗi xung đột" : "Không thể lưu", MessageBoxButton.OK, icon);
            return;
        }

        MessageBox.Show(result.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        HotelEventBus.PublishRoomStatusChanged();

        if (Mode == DatPhongMode.Normal)
            ResetFields();
        else
            CloseAction?.Invoke();
    }

    private string ValidateBeforeSave()
    {
        if (NgayCheckOut <= NgayCheckIn)
            return "Ngày check-out phải sau thời điểm check-in.";

        if (Mode == DatPhongMode.Normal && TienCoc < MinTienCoc)
            return $"Tiền cọc tối thiểu là {MinTienCoc:#,0}₫ (bằng giá 1 đêm).";

        if (Mode != DatPhongMode.GiaHan)
        {
            if (SelectedRoomsList.Count == 0)
                return "Vui lòng chọn ít nhất một phòng.";

            if (string.IsNullOrWhiteSpace(NewCustomer?.HoTen))
                return "Vui lòng nhập họ tên khách hàng.";

            var capacityError = ValidateSelectedRoomCapacities();
            if (!string.IsNullOrWhiteSpace(capacityError))
                return capacityError;
        }

        if (Mode == DatPhongMode.DoiPhong)
        {
            if (_chiTietDatPhong == null)
                return "Không tìm thấy thông tin đặt phòng cần đổi.";

            var newRoomItem = SelectedRoomsList.FirstOrDefault();
            if (newRoomItem == null)
                return "Vui lòng chọn phòng mới.";

            if (newRoomItem.MaPhong == _chiTietDatPhong.MaPhong)
                return "Vui lòng chọn phòng khác với phòng hiện tại.";
        }

        if (Mode == DatPhongMode.GiaHan && _chiTietDatPhong == null)
            return "Không tìm thấy thông tin đặt phòng cần gia hạn.";

        return string.Empty;
    }

    private string ValidateSelectedRoomCapacities()
    {
        using var ctx = new QuanLyKhachSanContext();

        foreach (var item in SelectedRoomsList)
        {
            var phong = ctx.Phongs.Include(p => p.LoaiPhong)
                                  .FirstOrDefault(p => p.MaPhong == item.MaPhong);
            if (phong != null && item.Capacity > phong.LoaiPhong.SoNguoiToiDa)
                return $"Phòng {item.RoomName} chỉ cho phép tối đa {phong.LoaiPhong.SoNguoiToiDa} người.";
        }

        return string.Empty;
    }

    private BookingRequestDTO CreateBookingRequest()
    {
        return new BookingRequestDTO
        {
            MaPhongList = SelectedRoomsList.Select(r => r.MaPhong).ToList(),
            NgayCheckIn = NgayCheckIn,
            NgayCheckOut = NgayCheckOut,
            HoTen = NewCustomer?.HoTen ?? string.Empty,
            SDT = NewCustomer?.SDT ?? string.Empty,
            CCCD = NewCustomer?.CCCD_Passport ?? string.Empty,
            GioiTinh = NewCustomer?.GioiTinh ?? string.Empty,
            QuocTich = NewCustomer?.QuocTich ?? string.Empty,
            TienCoc = TienCoc,
            IsWalkIn = Mode == DatPhongMode.WalkIn,
            MaNhanVien = LoginSession.CurrentNhanVienId
        };
    }

    private DoiPhongRequestDTO CreateDoiPhongRequest()
    {
        var newRoomItem = SelectedRoomsList.First();

        return new DoiPhongRequestDTO
        {
            MaDatPhong = _chiTietDatPhong.MaDatPhong,
            MaChiTietDatPhong = _chiTietDatPhong.MaChiTietDatPhong,
            MaPhongCu = _chiTietDatPhong.MaPhong,
            MaPhongMoi = newRoomItem.MaPhong,
            NgayCheckOut = NgayCheckOut,
            MaNhanVien = LoginSession.CurrentNhanVienId
        };
    }

    private GiaHanRequestDTO CreateGiaHanRequest()
    {
        return new GiaHanRequestDTO
        {
            MaChiTietDatPhong = _chiTietDatPhong.MaChiTietDatPhong,
            NgayCheckOutMoi = NgayCheckOut,
            MaNhanVien = LoginSession.CurrentNhanVienId
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void UpdateRoomCleaningStatus(Phong phong)
    {
        try
        {
            using var ctx = new QuanLyKhachSanContext();
            var phongDb = ctx.Phongs.Find(phong.MaPhong);
            if (phongDb != null)
            {
                phongDb.TrangThaiDonDep = 0;
                ctx.SaveChanges();
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

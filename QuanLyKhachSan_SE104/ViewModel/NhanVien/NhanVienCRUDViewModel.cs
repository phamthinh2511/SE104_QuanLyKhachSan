using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using NhanVienModel = QuanLyKhachSan_SE104.Model.NhanVien;

namespace QuanLyKhachSan_SE104.ViewModel.NhanVien
{
    public class NhanVienCRUDViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ModeNhanSu _mode;
        public ModeNhanSu Mode { get => _mode; set { _mode = value; OnPropertyChanged(); } }

        private NhanVienModel _nhanVien;
        public NhanVienModel NhanVien { get => _nhanVien; set { _nhanVien = value; OnPropertyChanged(); } }

        // BIẾN MAP VỚI COMBOBOX CHỨC VỤ (0 = Lễ tân, 1 = Quản lý)
        private int _chucVuIndex;
        public int ChucVuIndex { get => _chucVuIndex; set { _chucVuIndex = value; OnPropertyChanged(); } }

        private TaiKhoan _taiKhoan;
        public TaiKhoan TaiKhoan { get => _taiKhoan; set { _taiKhoan = value; OnPropertyChanged(); } }

        private ObservableCollection<NhanVienModel> _danhSachNhanVien;
        public ObservableCollection<NhanVienModel> DanhSachNhanVien { get => _danhSachNhanVien; set { _danhSachNhanVien = value; OnPropertyChanged(); } }

        public Action<object> OnSaved;
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void LoadNhanVien()
        {
            if (Mode == ModeNhanSu.TaiKhoan)
            {
                using (var context = new QuanLyKhachSanContext())
                {
                    // Lấy ID nhân viên hiện tại của tài khoản (nếu đang ở chế độ Sửa, nếu Thêm mới thì bằng 0)
                    int currentMaNV = TaiKhoan?.MaNhanVien ?? 0;

                    // Lọc: Lấy nv Đang làm việc (true) HOẶC là nv đang được gán cho tài khoản này
                    var ds = context.NhanViens
                        .Where(nv => nv.TrangThaiLamViec == true || nv.MaNhanVien == currentMaNV)
                        .ToList();

                    DanhSachNhanVien = new ObservableCollection<NhanVienModel>(ds);
                }
            }
        }

        // ===== ADD =====
        public NhanVienCRUDViewModel(ModeNhanSu mode)
        {
            Mode = mode;
            if (mode == ModeNhanSu.NhanVien)
            {
                NhanVien = new NhanVienModel { TrangThaiLamViec = true }; 
                ChucVuIndex = 0; // Mặc định là Lễ tân
            }
            else
                TaiKhoan = new TaiKhoan();

            LoadNhanVien();
            SaveCommand = new RelayCommand<object>(ExecuteSave);
            CancelCommand = new RelayCommand<object>(ExecuteCancel);
        }

        // ===== EDIT =====
        public NhanVienCRUDViewModel(object obj)
        {
            if (obj is NhanVienModel nv)
            {
                Mode = ModeNhanSu.NhanVien;
                NhanVien = new NhanVienModel
                {
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    Email = nv.Email,
                    SoDienThoai = nv.SoDienThoai,
                    CCCD = nv.CCCD,
                    ChucVu = nv.ChucVu,
                    TrangThaiLamViec = nv.TrangThaiLamViec
                };
                ChucVuIndex = nv.ChucVu ? 1 : 0; // Map bool vào ComboBox
            }
            else if (obj is TaiKhoan tk)
            {
                Mode = ModeNhanSu.TaiKhoan;
                TaiKhoan = new TaiKhoan
                {
                    MaTaiKhoan = tk.MaTaiKhoan,
                    Username = tk.Username,
                    PasswordHash = tk.PasswordHash,
                    MaNhanVien = tk.MaNhanVien
                };
            }
            else { throw new Exception("Không xác định loại dữ liệu"); }

            LoadNhanVien();
            SaveCommand = new RelayCommand<object>(ExecuteSave);
            CancelCommand = new RelayCommand<object>(ExecuteCancel);
        }

        private bool _isSaved = false;

        private void ExecuteSave(object obj)
        {
            if (_isSaved) return;

            // KIỂM TRA LỖI TRƯỚC KHI LƯU 
            if (Mode == ModeNhanSu.NhanVien)
            {
                if (string.IsNullOrWhiteSpace(NhanVien.HoTen))
                {
                    MessageBox.Show("Vui lòng nhập họ tên nhân viên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Gán lại chức vụ từ ComboBox vào DB
                NhanVien.ChucVu = ChucVuIndex == 1;
            }
            else if (Mode == ModeNhanSu.TaiKhoan)
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan.Username) || string.IsNullOrWhiteSpace(TaiKhoan.PasswordHash))
                {
                    MessageBox.Show("Vui lòng nhập Username và Password!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // MỌI THỨ OK THÌ MỚI LƯU
            _isSaved = true;

            if (Mode == ModeNhanSu.NhanVien)
                OnSaved?.Invoke(NhanVien);
            else
                OnSaved?.Invoke(TaiKhoan);

            CloseWindow(obj);
        }

        private void ExecuteCancel(object obj) => CloseWindow(obj);

        private void CloseWindow(object obj)
        {
            if (obj is System.Windows.Window win) win.Close();
        }
    }
}
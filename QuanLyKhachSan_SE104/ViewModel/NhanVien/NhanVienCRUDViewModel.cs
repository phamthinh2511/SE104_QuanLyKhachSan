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
            void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            // 🔥 FIX: phải có notify
            private ModeNhanSu _mode;
            public ModeNhanSu Mode
            {
                get => _mode;
                set { _mode = value; OnPropertyChanged(); }
            }

            private NhanVienModel _nhanVien;
            public NhanVienModel NhanVien
            {
                get => _nhanVien;
                set { _nhanVien = value; OnPropertyChanged(); }
            }

            private TaiKhoan _taiKhoan;
            public TaiKhoan TaiKhoan
            {
                get => _taiKhoan;
                set { _taiKhoan = value; OnPropertyChanged(); }
            }

            private ObservableCollection<NhanVienModel> _danhSachNhanVien;
            public ObservableCollection<NhanVienModel> DanhSachNhanVien
            {
                get => _danhSachNhanVien;
                set { _danhSachNhanVien = value; OnPropertyChanged(); }
            }

            public Action<object> OnSaved;

            public ICommand SaveCommand { get; }
            public ICommand CancelCommand { get; }

            private void LoadNhanVien()
            {
                if (Mode == ModeNhanSu.TaiKhoan)
                {
                    using (var context = new QuanLyKhachSanContext())
                    {
                        DanhSachNhanVien = new ObservableCollection<NhanVienModel>(context.NhanViens.ToList());
                    }
                }
            }

            // ===== ADD =====
            public NhanVienCRUDViewModel(ModeNhanSu mode)
            {
                Mode = mode;

                if (mode == ModeNhanSu.NhanVien)
                    NhanVien = new NhanVienModel();
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

                    // 🔥 FIX: clone để không sửa trực tiếp
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
                else
                {
                    throw new Exception("Không xác định loại dữ liệu");
                }

                LoadNhanVien();

                SaveCommand = new RelayCommand<object>(ExecuteSave);
                CancelCommand = new RelayCommand<object>(ExecuteCancel);
            }

        private bool _isSaved = false;

        private void ExecuteSave(object obj)
        {
            if (_isSaved) return;
            _isSaved = true;

            if (Mode == ModeNhanSu.NhanVien)
                OnSaved?.Invoke(NhanVien);
            else
                OnSaved?.Invoke(TaiKhoan);

            if (Mode == ModeNhanSu.NhanVien)
            {
                if (string.IsNullOrWhiteSpace(NhanVien.HoTen))
                {
                    MessageBox.Show("Chưa nhập tên!");
                    return;
                }
            }

            CloseWindow(obj);
        }

        private void ExecuteCancel(object obj)
            {
                CloseWindow(obj);
            }

            private void CloseWindow(object obj)
            {
                if (obj is System.Windows.Window win)
                    win.Close();
            }
        }
    }
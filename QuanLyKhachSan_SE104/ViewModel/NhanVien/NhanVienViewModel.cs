using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.NhanVien;
using NhanVienModel = QuanLyKhachSan_SE104.Model.NhanVien;

namespace QuanLyKhachSan_SE104.ViewModel.NhanVien
{
    public class NhanVienViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== MODE =====
        public ObservableCollection<ModeNhanSu> DanhSachMode { get; set; }
            = new ObservableCollection<ModeNhanSu>
        {
            ModeNhanSu.NhanVien,
            ModeNhanSu.TaiKhoan
        };

        private ModeNhanSu _modeDuocChon;
        public ModeNhanSu ModeDuocChon
        {
            get => _modeDuocChon;
            set { _modeDuocChon = value; OnPropertyChanged(); }
        }

        // ===== DATA =====
        public ObservableCollection<NhanVienModel> DanhSachNhanVien { get; set; }
        public ObservableCollection<TaiKhoan> DanhSachTaiKhoan { get; set; }

        // ===== UNDO =====
        private object _lastDeletedItem;

        public bool IsUndoVisible { get; set; }
        public string UndoMessage { get; set; }

        // ===== COMMAND =====
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand UndoCommand { get; }

        public NhanVienViewModel()
        {
            ModeDuocChon = ModeNhanSu.NhanVien;

            // Fake data
            DanhSachNhanVien = new ObservableCollection<NhanVienModel>
            {
                new NhanVienModel { MaNhanVien = 1, HoTen = "Nguyễn Văn A", ChucVu = true, TrangThaiLamViec = true },
                new NhanVienModel { MaNhanVien = 2, HoTen = "Trần Thị B", ChucVu = false, TrangThaiLamViec = true }
            };

            DanhSachTaiKhoan = new ObservableCollection<TaiKhoan>
            {
                new TaiKhoan { MaTaiKhoan = 1, Username = "admin", PasswordHash = "123", MaNhanVien = 1, NhanVien = DanhSachNhanVien[0] },
                new TaiKhoan { MaTaiKhoan = 2, Username = "user", PasswordHash = "123", MaNhanVien = 2, NhanVien = DanhSachNhanVien[1] }
            };

            AddCommand = new RelayCommand<object>(ExecuteAdd);
            EditCommand = new RelayCommand<object>(ExecuteEdit);
            DeleteCommand = new RelayCommand<object>(ExecuteDelete);
            UndoCommand = new RelayCommand<object>(ExecuteUndo);
        }

        // ===== ADD =====
        private void ExecuteAdd(object obj)
        {
            var vm = new NhanVienCRUDViewModel(ModeDuocChon);

            vm.OnSaved = (saved) =>
            {
                if (saved is NhanVienModel nv)
                {
                    nv.MaNhanVien = DanhSachNhanVien.Any()
                        ? DanhSachNhanVien.Max(x => x.MaNhanVien) + 1
                        : 1;

                    DanhSachNhanVien.Add(nv);
                }
                else if (saved is TaiKhoan tk)
                {
                    tk.MaTaiKhoan = DanhSachTaiKhoan.Any()
                        ? DanhSachTaiKhoan.Max(x => x.MaTaiKhoan) + 1
                        : 1;

                    DanhSachTaiKhoan.Add(tk);
                }
            };

            var win = new NhanVienCRUD { DataContext = vm };
            win.ShowDialog();
        }

        // ===== EDIT =====
        private void ExecuteEdit(object item)
        {
            if (item == null) return;

            var vm = new NhanVienCRUDViewModel(item);

            vm.OnSaved = (saved) =>
            {
                if (item is NhanVienModel oldNv && saved is NhanVienModel newNv)
                {
                    oldNv.HoTen = newNv.HoTen;
                    oldNv.ChucVu = newNv.ChucVu;
                    oldNv.TrangThaiLamViec = newNv.TrangThaiLamViec;
                }
                else if (item is TaiKhoan oldTk && saved is TaiKhoan newTk)
                {
                    oldTk.Username = newTk.Username;
                    oldTk.PasswordHash = newTk.PasswordHash;
                }
            };

            var win = new NhanVienCRUD { DataContext = vm };
            win.ShowDialog();
        }

        // ===== DELETE + UNDO =====
        private async void ExecuteDelete(object item)
        {
            if (item == null) return;

            _lastDeletedItem = item;

            if (item is NhanVienModel nv)
            {
                DanhSachNhanVien.Remove(nv);
                UndoMessage = $"Đã xóa: {nv.HoTen}";
            }
            else if (item is TaiKhoan tk)
            {
                DanhSachTaiKhoan.Remove(tk);
                UndoMessage = $"Đã xóa: {tk.Username}";
            }

            IsUndoVisible = true;
            OnPropertyChanged(nameof(IsUndoVisible));
            OnPropertyChanged(nameof(UndoMessage));

            await Task.Delay(5000);

            if (_lastDeletedItem == item)
            {
                IsUndoVisible = false;
                OnPropertyChanged(nameof(IsUndoVisible));
                _lastDeletedItem = null;
            }
        }

        private void ExecuteUndo(object obj)
        {
            if (_lastDeletedItem == null) return;

            if (_lastDeletedItem is NhanVienModel nv)
                DanhSachNhanVien.Add(nv);
            else if (_lastDeletedItem is TaiKhoan tk)
                DanhSachTaiKhoan.Add(tk);

            IsUndoVisible = false;
            OnPropertyChanged(nameof(IsUndoVisible));

            _lastDeletedItem = null;
        }
    }
}
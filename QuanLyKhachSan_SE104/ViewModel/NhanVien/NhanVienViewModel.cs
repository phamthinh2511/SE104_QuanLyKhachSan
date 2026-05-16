using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.NhanVienView;
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
        private ObservableCollection<NhanVienModel> _danhSachNhanVien;
        public ObservableCollection<NhanVienModel> DanhSachNhanVien
        {
            get => _danhSachNhanVien;
            set { _danhSachNhanVien = value; OnPropertyChanged(); }
        }

        private ObservableCollection<TaiKhoan> _danhSachTaiKhoan;
        public ObservableCollection<TaiKhoan> DanhSachTaiKhoan
        {
            get => _danhSachTaiKhoan;
            set { _danhSachTaiKhoan = value; OnPropertyChanged(); }
        }

        // ===== UNDO =====
        private object _lastDeletedItem;

        private bool _isUndoVisible;
        public bool IsUndoVisible
        {
            get => _isUndoVisible;
            set { _isUndoVisible = value; OnPropertyChanged(); }
        }

        private string _undoMessage;
        public string UndoMessage
        {
            get => _undoMessage;
            set { _undoMessage = value; OnPropertyChanged(); }
        }

        // ===== COMMAND =====
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand UndoCommand { get; }

        public NhanVienViewModel()
        {
            ModeDuocChon = ModeNhanSu.NhanVien;
            LoadData();

            AddCommand = new RelayCommand<object>(ExecuteAdd);
            EditCommand = new RelayCommand<object>(ExecuteEdit);
            DeleteCommand = new RelayCommand<object>(ExecuteDelete);
            UndoCommand = new RelayCommand<object>(ExecuteUndo);
        }

        public void LoadData()
        {
            using (var context = new QuanLyKhachSanContext())
            {
                DanhSachNhanVien = new ObservableCollection<NhanVienModel>(context.NhanViens.ToList());
                DanhSachTaiKhoan = new ObservableCollection<TaiKhoan>(context.TaiKhoans.Include(t => t.NhanVien).ToList());
            }
        }

        // ===== ADD =====
        private void ExecuteAdd(object obj)
        {
            var vm = new NhanVienCRUDViewModel(ModeDuocChon);

            vm.OnSaved = (saved) =>
            {
                try
                {
                    using (var context = new QuanLyKhachSanContext())
                    {
                        if (saved is NhanVienModel nv)
                        {
                            context.NhanViens.Add(nv);
                            context.SaveChanges();
                            MessageBox.Show("Thêm nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (saved is TaiKhoan tk)
                        {
                            tk.CreatedAt = System.DateTime.Now;
                            context.TaiKhoans.Add(tk);
                            context.SaveChanges();
                            MessageBox.Show("Thêm tài khoản thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    LoadData();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Thêm thất bại: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                try
                {
                    using (var context = new QuanLyKhachSanContext())
                    {
                        if (item is NhanVienModel oldNv && saved is NhanVienModel newNv)
                        {
                            var dbNv = context.NhanViens.Find(oldNv.MaNhanVien);
                            if (dbNv != null)
                            {
                                dbNv.HoTen = newNv.HoTen;
                                dbNv.Email = newNv.Email;
                                dbNv.SoDienThoai = newNv.SoDienThoai;
                                dbNv.CCCD = newNv.CCCD;
                                dbNv.ChucVu = newNv.ChucVu;
                                dbNv.TrangThaiLamViec = newNv.TrangThaiLamViec;
                                context.SaveChanges();
                                MessageBox.Show("Cập nhật nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else if (item is TaiKhoan oldTk && saved is TaiKhoan newTk)
                        {
                            var dbTk = context.TaiKhoans.Find(oldTk.MaTaiKhoan);
                            if (dbTk != null)
                            {
                                dbTk.Username = newTk.Username;
                                dbTk.PasswordHash = newTk.PasswordHash;
                                dbTk.MaNhanVien = newTk.MaNhanVien;
                                context.SaveChanges();
                                MessageBox.Show("Cập nhật tài khoản thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    LoadData();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Cập nhật thất bại: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                UndoMessage = $"Đã xóa nhân viên: {nv.HoTen}";
            }
            else if (item is TaiKhoan tk)
            {
                DanhSachTaiKhoan.Remove(tk);
                UndoMessage = $"Đã xóa tài khoản: {tk.Username}";
            }

            IsUndoVisible = true;

            // Wait 5 seconds for potential undo
            await Task.Delay(5000);

            if (_lastDeletedItem == item)
            {
                // Action was not undone, commit delete to database
                try
                {
                    using (var context = new QuanLyKhachSanContext())
                    {
                        if (item is NhanVienModel nvToDelete)
                        {
                            var dbNv = context.NhanViens.Find(nvToDelete.MaNhanVien);
                            if (dbNv != null)
                            {
                                context.NhanViens.Remove(dbNv);
                                context.SaveChanges();
                                MessageBox.Show("Xóa nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else if (item is TaiKhoan tkToDelete)
                        {
                            var dbTk = context.TaiKhoans.Find(tkToDelete.MaTaiKhoan);
                            if (dbTk != null)
                            {
                                context.TaiKhoans.Remove(dbTk);
                                context.SaveChanges();
                                MessageBox.Show("Xóa tài khoản thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Xóa thất bại: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsUndoVisible = false;
                    _lastDeletedItem = null;
                    LoadData();
                }
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
            _lastDeletedItem = null;
        }
    }
}
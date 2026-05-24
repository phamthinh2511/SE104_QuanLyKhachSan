using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
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
        void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<ModeNhanSu> DanhSachMode { get; set; } = new ObservableCollection<ModeNhanSu> { ModeNhanSu.NhanVien, ModeNhanSu.TaiKhoan };

        private ModeNhanSu _modeDuocChon;
        public ModeNhanSu ModeDuocChon { get => _modeDuocChon; set { _modeDuocChon = value; OnPropertyChanged(); } }

        private bool _isHienThiDaXoa;
        public bool IsHienThiDaXoa
        {
            get => _isHienThiDaXoa;
            set { _isHienThiDaXoa = value; OnPropertyChanged(); LoadData(); }
        }

        private ObservableCollection<NhanVienModel> _danhSachNhanVien;
        public ObservableCollection<NhanVienModel> DanhSachNhanVien { get => _danhSachNhanVien; set { _danhSachNhanVien = value; OnPropertyChanged(); } }

        private ObservableCollection<TaiKhoan> _danhSachTaiKhoan;
        public ObservableCollection<TaiKhoan> DanhSachTaiKhoan { get => _danhSachTaiKhoan; set { _danhSachTaiKhoan = value; OnPropertyChanged(); } }

        private object _lastDeletedItem;
        private bool _isUndoVisible;
        public bool IsUndoVisible { get => _isUndoVisible; set { _isUndoVisible = value; OnPropertyChanged(); } }
        private string _undoMessage;
        public string UndoMessage { get => _undoMessage; set { _undoMessage = value; OnPropertyChanged(); } }

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
                // Soft delete: false = đang làm (TrangThaiLamViec = true), true = đã nghỉ (TrangThaiLamViec = false)
                bool statusFilter = !IsHienThiDaXoa;
                DanhSachNhanVien = new ObservableCollection<NhanVienModel>(context.NhanViens.Where(nv => nv.TrangThaiLamViec == statusFilter).ToList());
                DanhSachTaiKhoan = new ObservableCollection<TaiKhoan>(context.TaiKhoans.Include(t => t.NhanVien).ToList());
            }
        }

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
                        }
                        else if (saved is TaiKhoan tk)
                        {
                            tk.CreatedAt = System.DateTime.Now;
                            context.TaiKhoans.Add(tk);
                            context.SaveChanges();
                        }
                    }
                    LoadData();
                }
                catch (System.Exception ex) { MessageBox.Show("Thêm thất bại: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };

            var win = new NhanVienCRUD { DataContext = vm };
            win.ShowDialog();
        }

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
                            }
                        }
                    }
                    LoadData();
                }
                catch (System.Exception ex) { MessageBox.Show("Cập nhật thất bại: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };

            var win = new NhanVienCRUD { DataContext = vm };
            win.ShowDialog();
        }

        private void ExecuteDelete(object item)
        {
            if (item == null) return;

            using (var context = new QuanLyKhachSanContext())
            {
                if (item is NhanVienModel nv)
                {
                    var dbNv = context.NhanViens.Find(nv.MaNhanVien);
                    if (dbNv != null)
                    {
                        if (IsHienThiDaXoa) // Đang ở danh sách nghỉ -> Nhấn nút là Khôi phục
                        {
                            dbNv.TrangThaiLamViec = true;
                            context.SaveChanges();
                            MessageBox.Show("Khôi phục nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else // Đang ở danh sách hoạt động -> Nhấn nút là Xóa 
                        {
                            var result = MessageBox.Show($"Bạn muốn xóa nhân viên {nv.HoTen}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Yes)
                            {
                                dbNv.TrangThaiLamViec = false;
                                context.SaveChanges();

                                _lastDeletedItem = nv;
                                UndoMessage = $"Đã xóa nhân viên: {nv.HoTen}";
                                IsUndoVisible = true;
                            }
                        }
                    }
                }
                else if (item is TaiKhoan tk)
                {
                    var result = MessageBox.Show($"Xóa tài khoản {tk.Username}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        var dbTk = context.TaiKhoans.Find(tk.MaTaiKhoan);
                        if (dbTk != null)
                        {
                            context.TaiKhoans.Remove(dbTk);
                            context.SaveChanges();
                        }
                    }
                }
            }
            LoadData();
        }

        private void ExecuteUndo(object obj)
        {
            if (_lastDeletedItem is NhanVienModel nv)
            {
                using (var context = new QuanLyKhachSanContext())
                {
                    var dbNv = context.NhanViens.Find(nv.MaNhanVien);
                    if (dbNv != null)
                    {
                        dbNv.TrangThaiLamViec = true; // Undo xóa mềm
                        context.SaveChanges();
                    }
                }
            }
            IsUndoVisible = false;
            _lastDeletedItem = null;
            LoadData();
        }
    }
}
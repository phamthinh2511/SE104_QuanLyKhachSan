using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices; // 👈 THÊM

namespace QuanLyKhachSan_SE104.ViewModel.DanhMuc
{
    public enum LoaiDanhMuc
    {
        LoaiPhong,
        Phong,
        DichVu,
        //KhachHang
    }

    public class DanhMucViewModel : INotifyPropertyChanged
    {
        // ─── INotifyPropertyChanged ✅ ─────────────────────
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ─── COMMAND CLICK ROOM CARD ✅ ────────────────────
        public ICommand MoChiTietPhongCommand { get; }

        // ─── COMBOBOX ─────────────────────────────────────
        public ObservableCollection<LoaiDanhMuc> DanhSachLoaiDanhMuc { get; set; }

        private LoaiDanhMuc _danhMucDuocChon;
        public LoaiDanhMuc DanhMucDuocChon
        {
            get => _danhMucDuocChon;
            set
            {
                _danhMucDuocChon = value;
                OnPropertyChanged();
                LoadDataForCategory();
            }
        }

        // ─── DATA ─────────────────────────────────────────
        private ObservableCollection<LoaiPhong> _danhSachLoaiPhong;
        public ObservableCollection<LoaiPhong> DanhSachLoaiPhong
        {
            get => _danhSachLoaiPhong;
            set { _danhSachLoaiPhong = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Phong> _danhSachPhong;
        public ObservableCollection<Phong> DanhSachPhong
        {
            get => _danhSachPhong;
            set { _danhSachPhong = value; OnPropertyChanged(); }
        }

        private ObservableCollection<DichVu> _danhSachDichVu;
        public ObservableCollection<DichVu> DanhSachDichVu
        {
            get => _danhSachDichVu;
            set { _danhSachDichVu = value; OnPropertyChanged(); }
        }

        //private ObservableCollection<KhachHang> _danhSachKhachHang;
        //public ObservableCollection<KhachHang> DanhSachKhachHang
        //{
        //    get => _danhSachKhachHang;
        //    set { _danhSachKhachHang = value; OnPropertyChanged(); }
        //}

        private bool _isHienThiDaXoa;
        public bool IsHienThiDaXoa
        {
            get => _isHienThiDaXoa;
            set
            {
                _isHienThiDaXoa = value;
                OnPropertyChanged();

                // Cứ mỗi lần lật Toggle, tự động tải lại dữ liệu cho bảng
                LoadDataForCategory();
            }
        }

        // ─── UNDO ─────────────────────────────────────────
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

        private object _lastDeletedItem;

        // ─── COMMANDS ─────────────────────────────────────
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand UndoCommand { get; }

        public DanhMucViewModel()
        {
            // 👇 COMMAND CLICK ROOM
            MoChiTietPhongCommand = new RelayCommand<Phong>(phong =>
            {
                if (phong == null) return;
                MessageBox.Show($"Mở chi tiết phòng: {phong.TenPhong}");
            });

            DanhSachLoaiDanhMuc = new ObservableCollection<LoaiDanhMuc>
            {
                LoaiDanhMuc.LoaiPhong,
                LoaiDanhMuc.Phong,
                LoaiDanhMuc.DichVu,
                //LoaiDanhMuc.KhachHang
            };

            DanhMucDuocChon = LoaiDanhMuc.LoaiPhong;

            AddNewCommand = new RelayCommand<object>(ExecuteAddNew);
            EditCommand = new RelayCommand<object>(ExecuteEdit);
            DeleteCommand = new RelayCommand<object>(ExecuteDelete);
            UndoCommand = new RelayCommand<object>(ExecuteUndo);
        }

        private void LoadDataForCategory()
        {
            var loaiPhongDAL = new DAL.QuanLyLoaiPhongDAL();
            var phongDAL = new DAL.QuanLyPhongDAL();
            var dichVuDAL = new DAL.QuanLyDichVuDAL();
            var khachHangDAL = new DAL.QuanLyKhachHangDAL();

            switch (DanhMucDuocChon)
            {
                case LoaiDanhMuc.LoaiPhong:
                    var tatCaLoai = loaiPhongDAL.LayDanhSachTatCa();
                    // Lọc ra các mục có trạng thái IsDeleted khớp với nút Toggle
                    DanhSachLoaiPhong = new ObservableCollection<LoaiPhong>(
                        tatCaLoai.Where(x => x.IsDeleted == IsHienThiDaXoa));
                    break;

                case LoaiDanhMuc.Phong:
                    var tatCaPhong = phongDAL.LayDanhSachTatCa();
                    DanhSachPhong = new ObservableCollection<Phong>(
                        tatCaPhong.Where(x => x.IsDeleted == IsHienThiDaXoa));
                    break;

                case LoaiDanhMuc.DichVu:
                    var tatCaDV = dichVuDAL.LayDanhSachTatCa();
                    DanhSachDichVu = new ObservableCollection<DichVu>(
                        tatCaDV.Where(x => x.IsDeleted == IsHienThiDaXoa));
                    break;
            }
        }

        private void ExecuteAddNew(object obj)
        {
            if (DanhMucDuocChon == LoaiDanhMuc.Phong)
            {
                var loaiPhongDAL = new DAL.QuanLyLoaiPhongDAL();
                DanhSachLoaiPhong = new ObservableCollection<LoaiPhong>(loaiPhongDAL.LayDanhSachActive());
            }

            var vm = new DanhMucCRUDViewModel(DanhMucDuocChon, DanhSachLoaiPhong);
            vm.OnSaved = (saved) =>
            {
                LoadDataForCategory(); 
            };
            var win = new View.DanhMuc.DanhMucCRUD(vm);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void ExecuteEdit(object item)
        {
            if (item == null) return;

            DanhMucCRUDViewModel vm = item switch
            {
                LoaiPhong lp => new DanhMucCRUDViewModel(lp),
                Phong p => new DanhMucCRUDViewModel(p, DanhSachLoaiPhong),
                DichVu dv => new DanhMucCRUDViewModel(dv),
                //KhachHang kh => new DanhMucCRUDViewModel(kh),
                _ => null
            };

            if (vm == null) return;

            // Sửa thì không cần Add vào list — object gốc đã được cập nhật trực tiếp
            // Nhưng cần notify DataGrid refresh
            vm.OnSaved = (_) => LoadDataForCategory();

            var win = new View.DanhMuc.DanhMucCRUD(vm);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private async void ExecuteDelete(object item)
        {
            if (item == null) return;
            _lastDeletedItem = item;

            var loaiPhongDAL = new DAL.QuanLyLoaiPhongDAL();
            var phongDAL = new DAL.QuanLyPhongDAL();
            var dichVuDAL = new DAL.QuanLyDichVuDAL();
            var khachHangDAL = new DAL.QuanLyKhachHangDAL();

            if (item is Phong p)
            {
                if (p.IsDeleted) // Màn hình ĐÃ XÓA -> Nút này đóng vai trò KHÔI PHỤC
                {
                    if (phongDAL.HoanTacXoa(p.MaPhong))
                    {
                        DanhSachPhong.Remove(p); 
                        MessageBox.Show($"Đã khôi phục phòng {p.TenPhong} thành công!", "Khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else // Màn hình HOẠT ĐỘNG -> Nút này đóng vai trò XÓA
                {
                    string errorMsg = phongDAL.KiemTraDieuKienXoa(p.MaPhong);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        MessageBox.Show(errorMsg, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng {p.TenPhong}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) return;

                    if (phongDAL.Xoa(p.MaPhong))
                    {
                        DanhSachPhong.Remove(p); 
                        _lastDeletedItem = p;
                        UndoMessage = $"Đã xóa Phòng: {p.TenPhong}";
                        IsUndoVisible = true;
                    }
                }
            }
            else if (item is LoaiPhong lp)
            {
                if (lp.IsDeleted)
                {
                    if (loaiPhongDAL.HoanTacXoa(lp.MaLoaiPhong))
                    {
                        DanhSachLoaiPhong.Remove(lp);
                        MessageBox.Show($"Đã khôi phục {lp.TenLoaiPhong} thành công!", "Khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    string errorMsg = loaiPhongDAL.KiemTraDieuKienXoa(lp.MaLoaiPhong);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        MessageBox.Show(errorMsg, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa loại phòng {lp.TenLoaiPhong}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) return;

                    if (loaiPhongDAL.Xoa(lp.MaLoaiPhong))
                    {
                        DanhSachLoaiPhong.Remove(lp);
                        _lastDeletedItem = lp;
                        UndoMessage = $"Đã xóa Loại phòng: {lp.TenLoaiPhong}";
                        IsUndoVisible = true;
                    }
                }
            }
            else if (item is DichVu dv)
            {
                if (dv.IsDeleted) // Đang ở chế độ xem đã xóa -> Khôi phục
                {
                    if (dichVuDAL.HoanTacXoa(dv.MaDichVu))
                    {
                        DanhSachDichVu.Remove(dv);
                        MessageBox.Show($"Đã khôi phục dịch vụ {dv.TenDichVu} thành công!", "Khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else // Đang hoạt động -> Xóa
                {
                    string errorMsg = dichVuDAL.KiemTraDieuKienXoa(dv.MaDichVu);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        MessageBox.Show(errorMsg, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa dịch vụ {dv.TenDichVu}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) return;

                    if (dichVuDAL.Xoa(dv.MaDichVu))
                    {
                        DanhSachDichVu.Remove(dv);
                        _lastDeletedItem = dv;
                        UndoMessage = $"Đã xóa Dịch vụ: {dv.TenDichVu}";
                        IsUndoVisible = true;
                    }
                }
            }

            if (IsUndoVisible)
            {
                await Task.Delay(5000);

                if (_lastDeletedItem == item)
                {
                    IsUndoVisible = false;
                    _lastDeletedItem = null;
                }
            }
        }

        private void ExecuteUndo(object obj)
        {
            if (_lastDeletedItem == null) return;

            var loaiPhongDAL = new DAL.QuanLyLoaiPhongDAL();
            var phongDAL = new DAL.QuanLyPhongDAL();
            var dichVuDAL = new DAL.QuanLyDichVuDAL();
            var khachHangDAL = new DAL.QuanLyKhachHangDAL();

            bool isSuccess = false;

            if (_lastDeletedItem is Phong p)
            {
                isSuccess = phongDAL.HoanTacXoa(p.MaPhong);
            }
            else if (_lastDeletedItem is LoaiPhong lp)
            {
                isSuccess = loaiPhongDAL.HoanTacXoa(lp.MaLoaiPhong);
            }
            else if (_lastDeletedItem is DichVu dv)
            {
                isSuccess = dichVuDAL.HoanTacXoa(dv.MaDichVu);
            }

            if (isSuccess)
            {
                LoadDataForCategory();
            }

            IsUndoVisible = false;
            _lastDeletedItem = null;
        }
    }
}
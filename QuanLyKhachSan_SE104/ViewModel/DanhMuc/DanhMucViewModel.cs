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
        KhachHang
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

        private ObservableCollection<KhachHang> _danhSachKhachHang;
        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get => _danhSachKhachHang;
            set { _danhSachKhachHang = value; OnPropertyChanged(); }
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
                LoaiDanhMuc.KhachHang
            };

            DanhMucDuocChon = LoaiDanhMuc.LoaiPhong;

            AddNewCommand = new RelayCommand<object>(ExecuteAddNew);
            EditCommand = new RelayCommand<object>(ExecuteEdit);
            DeleteCommand = new RelayCommand<object>(ExecuteDelete);
            UndoCommand = new RelayCommand<object>(ExecuteUndo);
        }

        private void LoadDataForCategory()
        {
            using (var context = new QuanLyKhachSanContext())
            {
                switch (DanhMucDuocChon)
                {
                    case LoaiDanhMuc.LoaiPhong:
                        DanhSachLoaiPhong = new ObservableCollection<LoaiPhong>(
                            context.LoaiPhongs.Where(x => !x.IsDeleted).ToList());
                        break;

                    case LoaiDanhMuc.Phong:
                        DanhSachPhong = new ObservableCollection<Phong>(
                            context.Phongs.Include(p => p.LoaiPhong)
                            .Where(x => !x.IsDeleted).ToList());
                        break;

                    case LoaiDanhMuc.DichVu:
                        DanhSachDichVu = new ObservableCollection<DichVu>(
                            context.DichVus.Where(x => !x.IsDeleted).ToList());
                        break;

                    case LoaiDanhMuc.KhachHang:
                        DanhSachKhachHang = new ObservableCollection<KhachHang>(
                            context.KhachHangs.Where(x => !x.IsDeleted).ToList());
                        break;
                }
            }
        }

        private void ExecuteAddNew(object obj)
        {
            var vm = new DanhMucCRUDViewModel(DanhMucDuocChon, DanhSachLoaiPhong);
            vm.OnSaved = (saved) =>
            {
                switch (DanhMucDuocChon)
                {
                    case LoaiDanhMuc.LoaiPhong:
                        DanhSachLoaiPhong.Add((LoaiPhong)saved); break;
                    case LoaiDanhMuc.Phong:
                        DanhSachPhong.Add((Phong)saved); break;
                    case LoaiDanhMuc.DichVu:
                        DanhSachDichVu.Add((DichVu)saved); break;
                    case LoaiDanhMuc.KhachHang:
                        DanhSachKhachHang.Add((KhachHang)saved); break;
                }
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
                KhachHang kh => new DanhMucCRUDViewModel(kh),
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

            using (var context = new QuanLyKhachSanContext())
            {
                _lastDeletedItem = item;

                if (item is Phong p)
                {
                    var entity = context.Phongs.Find(p.MaPhong);
                    if (entity != null) { entity.IsDeleted = true; context.SaveChanges(); }
                    DanhSachPhong.Remove(p);
                    UndoMessage = $"Đã xóa Phòng: {p.TenPhong}";
                }
                else if (item is LoaiPhong lp)
                {
                    var entity = context.LoaiPhongs.Find(lp.MaLoaiPhong);
                    if (entity != null) { entity.IsDeleted = true; context.SaveChanges(); }
                    DanhSachLoaiPhong.Remove(lp);
                    UndoMessage = $"Đã xóa Loại phòng: {lp.TenLoaiPhong}";
                }
                else if (item is DichVu dv)
                {
                    var entity = context.DichVus.Find(dv.MaDichVu);
                    if (entity != null) { entity.IsDeleted = true; context.SaveChanges(); }
                    DanhSachDichVu.Remove(dv);
                    UndoMessage = $"Đã xóa Dịch vụ: {dv.TenDichVu}";
                }
                else if (item is KhachHang kh)
                {
                    var entity = context.KhachHangs.Find(kh.MaKhachHang);
                    if (entity != null) { entity.IsDeleted = true; context.SaveChanges(); }
                    DanhSachKhachHang.Remove(kh);
                    UndoMessage = $"Đã xóa Khách hàng: {kh.HoTen}";
                }
            }

            IsUndoVisible = true;
            await Task.Delay(5000);

            if (_lastDeletedItem == item)
            {
                IsUndoVisible = false;
                _lastDeletedItem = null;
            }
        }

        private void ExecuteUndo(object obj)
        {
            if (_lastDeletedItem == null) return;

            using (var context = new QuanLyKhachSanContext())
            {
                if (_lastDeletedItem is Phong p)
                {
                    var entity = context.Phongs.Find(p.MaPhong);
                    if (entity != null) { entity.IsDeleted = false; context.SaveChanges(); }
                    DanhSachPhong.Add(p);
                }
                else if (_lastDeletedItem is LoaiPhong lp)
                {
                    var entity = context.LoaiPhongs.Find(lp.MaLoaiPhong);
                    if (entity != null) { entity.IsDeleted = false; context.SaveChanges(); }
                    DanhSachLoaiPhong.Add(lp);
                }
                else if (_lastDeletedItem is DichVu dv)
                {
                    var entity = context.DichVus.Find(dv.MaDichVu);
                    if (entity != null) { entity.IsDeleted = false; context.SaveChanges(); }
                    DanhSachDichVu.Add(dv);
                }
                else if (_lastDeletedItem is KhachHang kh)
                {
                    var entity = context.KhachHangs.Find(kh.MaKhachHang);
                    if (entity != null) { entity.IsDeleted = false; context.SaveChanges(); }
                    DanhSachKhachHang.Add(kh);
                }
            }

            IsUndoVisible = false;
            _lastDeletedItem = null;
        }
    }
}
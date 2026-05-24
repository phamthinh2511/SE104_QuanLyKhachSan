using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.DanhMuc
{
    public class DanhMucCRUDViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Action CloseAction { get; set; }

        // ── Chế độ & tiêu đề ────────────────────────────
        private readonly LoaiDanhMuc _loai;
        private readonly bool _isEdit;

        public string TieuDe { get; }
        public string NhanLuu { get; }

        // ── Visibility theo loại ────────────────────────
        public bool IsLoaiPhong => _loai == LoaiDanhMuc.LoaiPhong;
        public bool IsPhong => _loai == LoaiDanhMuc.Phong;
        public bool IsDichVu => _loai == LoaiDanhMuc.DichVu;
        //public bool IsKhachHang => _loai == LoaiDanhMuc.KhachHang;

        // ── Data gốc (để lưu) ───────────────────────────
        private readonly LoaiPhong _loaiPhongGoc;
        private readonly Phong _phongGoc;
        private readonly DichVu _dichVuGoc;
        private readonly KhachHang _khachHangGoc;

        // ── ComboBox Loại phòng (dùng cho form Phòng) ───
        public ObservableCollection<LoaiPhong> DanhSachLoaiPhong { get; }

        // ── Fields: LoaiPhong ────────────────────────────
        private string _tenLoaiPhong;
        public string TenLoaiPhong
        {
            get => _tenLoaiPhong;
            set { _tenLoaiPhong = value; OnPropertyChanged(); }
        }

        private decimal _giaMacDinh;
        public decimal GiaMacDinh
        {
            get => _giaMacDinh;
            set { _giaMacDinh = value; OnPropertyChanged(); }
        }

        private decimal _phuPhiThemGio;
        public decimal PhuPhiThemGio
        {
            get => _phuPhiThemGio;
            set { _phuPhiThemGio = value; OnPropertyChanged(); }
        }

        private int _soGiuong;
        public int SoGiuong
        {
            get => _soGiuong;
            set { _soGiuong = value; OnPropertyChanged(); }
        }

        private int _soNguoiToiDa;
        public int SoNguoiToiDa
        {
            get => _soNguoiToiDa;
            set { _soNguoiToiDa = value; OnPropertyChanged(); }
        }

        // ── Fields: Phong ────────────────────────────────
        private string _tenPhong;
        public string TenPhong
        {
            get => _tenPhong;
            set { _tenPhong = value; OnPropertyChanged(); }
        }

        private int _maLoaiPhong;
        public int MaLoaiPhong
        {
            get => _maLoaiPhong;
            set { _maLoaiPhong = value; OnPropertyChanged(); }
        }

        private int _soTang;
        public int SoTang
        {
            get => _soTang;
            set { _soTang = value; OnPropertyChanged(); }
        }

        // ── Fields: DichVu ───────────────────────────────
        private string _tenDichVu;
        public string TenDichVu
        {
            get => _tenDichVu;
            set { _tenDichVu = value; OnPropertyChanged(); }
        }

        private int _loaiDichVu;
        public int LoaiDichVu
        {
            get => _loaiDichVu;
            set { _loaiDichVu = value; OnPropertyChanged(); }
        }

        private decimal _donGia;
        public decimal DonGia
        {
            get => _donGia;
            set { _donGia = value; OnPropertyChanged(); }
        }

        private string _moTa;
        public string MoTa
        {
            get => _moTa;
            set { _moTa = value; OnPropertyChanged(); }
        }

        // ── Fields: KhachHang ────────────────────────────
        private string _hoTen;
        public string HoTen
        {
            get => _hoTen;
            set { _hoTen = value; OnPropertyChanged(); }
        }

        private string _gioiTinh;
        public string GioiTinh
        {
            get => _gioiTinh;
            set { _gioiTinh = value; OnPropertyChanged(); }
        }

        private string _quocTich;
        public string QuocTich
        {
            get => _quocTich;
            set { _quocTich = value; OnPropertyChanged(); }
        }

        private string _cccd_Passport;
        public string CCCD_Passport
        {
            get => _cccd_Passport;
            set { _cccd_Passport = value; OnPropertyChanged(); }
        }

        private string _sdt;
        public string SDT
        {
            get => _sdt;
            set { _sdt = value; OnPropertyChanged(); }
        }

        private string _diaChi;
        public string DiaChi
        {
            get => _diaChi;
            set { _diaChi = value; OnPropertyChanged(); }
        }

        // ── Commands ─────────────────────────────────────
        public ICommand LuuCommand { get; }
        public ICommand ThoatCommand { get; }

        // ── Callback ra ngoài sau khi lưu ───────────────
        public Action<object> OnSaved { get; set; }

        // ══════════════════════════════════════════════════
        // CONSTRUCTOR — THÊM MỚI
        // ══════════════════════════════════════════════════
        public DanhMucCRUDViewModel(LoaiDanhMuc loai,
                                    ObservableCollection<LoaiPhong> danhSachLoaiPhong = null)
        {
            _loai = loai;
            _isEdit = false;

            TieuDe = $"Thêm {TenLoai(loai)}";
            NhanLuu = "💾  Thêm mới";

            DanhSachLoaiPhong = danhSachLoaiPhong ?? new ObservableCollection<LoaiPhong>();

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        // ══════════════════════════════════════════════════
        // CONSTRUCTOR — SỬA
        // ══════════════════════════════════════════════════
        public DanhMucCRUDViewModel(LoaiPhong item)
        {
            _loai = LoaiDanhMuc.LoaiPhong;
            _isEdit = true;
            _loaiPhongGoc = item;

            TieuDe = "Sửa Loại Phòng";
            NhanLuu = "💾  Lưu thay đổi";

            // Điền dữ liệu vào     
            TenLoaiPhong = item.TenLoaiPhong;
            GiaMacDinh = item.GiaMacDinh;
            PhuPhiThemGio = item.PhuPhiThemGio;
            SoGiuong = item.SoGiuong;
            SoNguoiToiDa = item.SoNguoiToiDa;

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public DanhMucCRUDViewModel(Phong item,
                                    ObservableCollection<LoaiPhong> danhSachLoaiPhong)
        {
            _loai = LoaiDanhMuc.Phong;
            _isEdit = true;
            _phongGoc = item;

            TieuDe = "Sửa Phòng";
            NhanLuu = "💾  Lưu thay đổi";

            DanhSachLoaiPhong = danhSachLoaiPhong;

            TenPhong = item.TenPhong;
            MaLoaiPhong = item.MaLoaiPhong;
            SoTang = item.SoTang;

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public DanhMucCRUDViewModel(DichVu item)
        {
            _loai = LoaiDanhMuc.DichVu;
            _isEdit = true;
            _dichVuGoc = item;

            TieuDe = "Sửa Dịch Vụ";
            NhanLuu = "💾  Lưu thay đổi";

            TenDichVu = item.TenDichVu;
            LoaiDichVu = item.LoaiDichVu;
            DonGia = item.DonGia;
            MoTa = item.MoTa;

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        //public DanhMucCRUDViewModel(KhachHang item)
        //{
        //    _loai = LoaiDanhMuc.KhachHang;
        //    _isEdit = true;
        //    _khachHangGoc = item;

        //    TieuDe = "Sửa Khách Hàng";
        //    NhanLuu = "💾  Lưu thay đổi";

        //    HoTen = item.HoTen;
        //    GioiTinh = item.GioiTinh;
        //    QuocTich = item.QuocTich;
        //    CCCD_Passport = item.CCCD_Passport;
        //    SDT = item.SDT;
        //    DiaChi = item.DiaChi;

        //    LuuCommand = new RelayCommand(ExecuteLuu);
        //    ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        //}

        // ── Thực hiện lưu ───────────────────────────────
        private void ExecuteLuu()
        {
            var loaiPhongDAL = new DAL.QuanLyLoaiPhongDAL();
            var phongDAL = new DAL.QuanLyPhongDAL();

            switch (_loai)
            {
                case LoaiDanhMuc.LoaiPhong:
                    if (string.IsNullOrWhiteSpace(TenLoaiPhong))
                    {
                        MessageBox.Show("Vui lòng nhập tên loại phòng.", "Thiếu thông tin");
                        return;
                    }

                    if(GiaMacDinh <= 0)
                    {
                        MessageBox.Show("Vui lòng nhập giá mặc định.", "Thông tin không hợp lệ");
                        return;
                    }

                    if (SoGiuong <=0)
                    {
                        MessageBox.Show("Vui lòng nhập số giường.", "Thông tin không hợp lệ");
                        return;
                    }

                    if (SoNguoiToiDa <= 0)
                    {
                        MessageBox.Show("Vui lòng nhập số người tối đa.", "Thông tin không hợp lệ");
                        return;
                    }


                    if (_isEdit)
                    {
                        // Cập nhật thuộc tính của object gốc
                        _loaiPhongGoc.TenLoaiPhong = TenLoaiPhong;
                        _loaiPhongGoc.GiaMacDinh = GiaMacDinh;
                        _loaiPhongGoc.PhuPhiThemGio = PhuPhiThemGio;
                        _loaiPhongGoc.SoGiuong = SoGiuong;
                        _loaiPhongGoc.SoNguoiToiDa = SoNguoiToiDa;

                        // Gọi DAL để lưu vào DB
                        if (loaiPhongDAL.Sua(_loaiPhongGoc))
                        {
                            OnSaved?.Invoke(_loaiPhongGoc);
                        }
                    }
                    else
                    {
                        var newItem = new LoaiPhong
                        {
                            TenLoaiPhong = TenLoaiPhong,
                            GiaMacDinh = GiaMacDinh,
                            PhuPhiThemGio = PhuPhiThemGio,
                            SoGiuong = SoGiuong,
                            SoNguoiToiDa = SoNguoiToiDa
                        };

                        if (loaiPhongDAL.Them(newItem))
                        {
                            OnSaved?.Invoke(newItem);
                        }
                    }
                    break;

                case LoaiDanhMuc.Phong:
                    if (string.IsNullOrWhiteSpace(TenPhong))
                    {
                        MessageBox.Show("Vui lòng nhập tên phòng.", "Thiếu thông tin");
                        return;
                    }

                    if (MaLoaiPhong <= 0)
                    { MessageBox.Show("Vui lòng chọn loại phòng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                    // Ràng buộc Tầng
                    if (SoTang <= 0)
                    { MessageBox.Show("Số tầng phải lớn hơn 0.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                    if (_isEdit)
                    {
                        _phongGoc.TenPhong = TenPhong;
                        _phongGoc.MaLoaiPhong = MaLoaiPhong;
                        _phongGoc.SoTang = SoTang;
                        _phongGoc.LoaiPhong = DanhSachLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == MaLoaiPhong);

                        if (phongDAL.Sua(_phongGoc))
                        {
                            OnSaved?.Invoke(_phongGoc);
                        }
                    }
                    else
                    {
                        var newItem = new Phong
                        {
                            TenPhong = TenPhong,
                            MaLoaiPhong = MaLoaiPhong,
                            SoTang = SoTang,

                            LoaiPhong = DanhSachLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == MaLoaiPhong)
                        };

                        if (phongDAL.Them(newItem))
                        {
                            OnSaved?.Invoke(newItem);
                        }
                    }
                    break;

                case LoaiDanhMuc.DichVu:
                    if (string.IsNullOrWhiteSpace(TenDichVu))
                    { MessageBox.Show("Vui lòng nhập tên dịch vụ.", "Thiếu thông tin"); return; }

                    if (DonGia <= 0)
                    { MessageBox.Show("Đơn giá dịch vụ phải lớn hơn 0.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                    var dichVuDAL = new DAL.QuanLyDichVuDAL(); 

                    if (_isEdit)
                    {
                        _dichVuGoc.TenDichVu = TenDichVu;
                        _dichVuGoc.LoaiDichVu = LoaiDichVu;
                        _dichVuGoc.DonGia = DonGia;
                        _dichVuGoc.MoTa = MoTa;

                        if (dichVuDAL.Sua(_dichVuGoc))
                        {
                            OnSaved?.Invoke(_dichVuGoc);
                        }
                    }
                    else
                    {
                        var newItem = new DichVu
                        {
                            TenDichVu = TenDichVu,
                            LoaiDichVu = LoaiDichVu,
                            DonGia = DonGia,
                            MoTa = MoTa
                        };

                        if (dichVuDAL.Them(newItem))
                        {
                            OnSaved?.Invoke(newItem);
                        }
                    }
                    break;

            }               

            CloseAction?.Invoke();
        }

        private static string TenLoai(LoaiDanhMuc loai) => loai switch
        {
            LoaiDanhMuc.LoaiPhong => "Loại Phòng",
            LoaiDanhMuc.Phong => "Phòng",
            LoaiDanhMuc.DichVu => "Dịch Vụ",
            //LoaiDanhMuc.KhachHang => "Khách Hàng",
            _ => ""
        };
    }
}
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
        public bool IsKhachHang => _loai == LoaiDanhMuc.KhachHang;

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

        public DanhMucCRUDViewModel(KhachHang item)
        {
            _loai = LoaiDanhMuc.KhachHang;
            _isEdit = true;
            _khachHangGoc = item;

            TieuDe = "Sửa Khách Hàng";
            NhanLuu = "💾  Lưu thay đổi";

            HoTen = item.HoTen;
            GioiTinh = item.GioiTinh;
            QuocTich = item.QuocTich;
            CCCD_Passport = item.CCCD_Passport;
            SDT = item.SDT;
            DiaChi = item.DiaChi;

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        // ── Thực hiện lưu ───────────────────────────────
        private void ExecuteLuu()
        {
            using (var context = new QuanLyKhachSanContext())
            {
                switch (_loai)
                {
                    case LoaiDanhMuc.LoaiPhong:
                        if (string.IsNullOrWhiteSpace(TenLoaiPhong))
                        { MessageBox.Show("Vui lòng nhập tên loại phòng.", "Thiếu thông tin"); return; }

                        if (_isEdit)
                        {
                            var e = context.LoaiPhongs.Find(_loaiPhongGoc.MaLoaiPhong);
                            if (e != null)
                            {
                                e.TenLoaiPhong = TenLoaiPhong;
                                e.GiaMacDinh = GiaMacDinh;
                                e.PhuPhiThemGio = PhuPhiThemGio;
                                e.SoGiuong = SoGiuong;
                                e.SoNguoiToiDa = SoNguoiToiDa;
                                context.SaveChanges();

                                // Cập nhật object gốc để DataGrid tự refresh
                                _loaiPhongGoc.TenLoaiPhong = e.TenLoaiPhong;
                                _loaiPhongGoc.GiaMacDinh = e.GiaMacDinh;
                                _loaiPhongGoc.PhuPhiThemGio = e.PhuPhiThemGio;
                                _loaiPhongGoc.SoGiuong = e.SoGiuong;
                                _loaiPhongGoc.SoNguoiToiDa = e.SoNguoiToiDa;
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
                            context.LoaiPhongs.Add(newItem);
                            context.SaveChanges();
                            OnSaved?.Invoke(newItem);
                        }
                        break;

                    case LoaiDanhMuc.Phong:
                        if (string.IsNullOrWhiteSpace(TenPhong))
                        { MessageBox.Show("Vui lòng nhập tên phòng.", "Thiếu thông tin"); return; }

                        if (_isEdit)
                        {
                            var e = context.Phongs.Find(_phongGoc.MaPhong);
                            if (e != null)
                            {
                                e.TenPhong = TenPhong;
                                e.MaLoaiPhong = MaLoaiPhong;
                                e.SoTang = SoTang;
                                context.SaveChanges();

                                _phongGoc.TenPhong = e.TenPhong;
                                _phongGoc.MaLoaiPhong = e.MaLoaiPhong;
                                _phongGoc.SoTang = e.SoTang;
                                OnSaved?.Invoke(_phongGoc);
                            }
                        }
                        else
                        {
                            var newItem = new Phong
                            {
                                TenPhong = TenPhong,
                                MaLoaiPhong = MaLoaiPhong,
                                SoTang = SoTang
                            };
                            context.Phongs.Add(newItem);
                            context.SaveChanges();
                            OnSaved?.Invoke(newItem);
                        }
                        break;

                    case LoaiDanhMuc.DichVu:
                        if (string.IsNullOrWhiteSpace(TenDichVu))
                        { MessageBox.Show("Vui lòng nhập tên dịch vụ.", "Thiếu thông tin"); return; }

                        if (_isEdit)
                        {
                            var e = context.DichVus.Find(_dichVuGoc.MaDichVu);
                            if (e != null)
                            {
                                e.TenDichVu = TenDichVu;
                                e.LoaiDichVu = LoaiDichVu;
                                e.DonGia = DonGia;
                                e.MoTa = MoTa;
                                context.SaveChanges();

                                _dichVuGoc.TenDichVu = e.TenDichVu;
                                _dichVuGoc.LoaiDichVu = e.LoaiDichVu;
                                _dichVuGoc.DonGia = e.DonGia;
                                _dichVuGoc.MoTa = e.MoTa;
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
                            context.DichVus.Add(newItem);
                            context.SaveChanges();
                            OnSaved?.Invoke(newItem);
                        }
                        break;

                    case LoaiDanhMuc.KhachHang:
                        if (string.IsNullOrWhiteSpace(HoTen))
                        { MessageBox.Show("Vui lòng nhập họ tên.", "Thiếu thông tin"); return; }

                        if (_isEdit)
                        {
                            var e = context.KhachHangs.Find(_khachHangGoc.MaKhachHang);
                            if (e != null)
                            {
                                e.HoTen = HoTen;
                                e.GioiTinh = GioiTinh;
                                e.QuocTich = QuocTich;
                                e.CCCD_Passport = CCCD_Passport;
                                e.SDT = SDT;
                                e.DiaChi = DiaChi;
                                context.SaveChanges();

                                _khachHangGoc.HoTen = e.HoTen;
                                _khachHangGoc.GioiTinh = e.GioiTinh;
                                _khachHangGoc.QuocTich = e.QuocTich;
                                _khachHangGoc.CCCD_Passport = e.CCCD_Passport;
                                _khachHangGoc.SDT = e.SDT;
                                _khachHangGoc.DiaChi = e.DiaChi;
                                OnSaved?.Invoke(_khachHangGoc);
                            }
                        }
                        else
                        {
                            var newItem = new KhachHang
                            {
                                HoTen = HoTen,
                                GioiTinh = GioiTinh,
                                QuocTich = QuocTich,
                                CCCD_Passport = CCCD_Passport,
                                SDT = SDT,
                                DiaChi = DiaChi
                            };
                            context.KhachHangs.Add(newItem);
                            context.SaveChanges();
                            OnSaved?.Invoke(newItem);
                        }
                        break;
                }
            }

            CloseAction?.Invoke();
        }

        private static string TenLoai(LoaiDanhMuc loai) => loai switch
        {
            LoaiDanhMuc.LoaiPhong => "Loại Phòng",
            LoaiDanhMuc.Phong => "Phòng",
            LoaiDanhMuc.DichVu => "Dịch Vụ",
            LoaiDanhMuc.KhachHang => "Khách Hàng",
            _ => ""
        };
    }
}
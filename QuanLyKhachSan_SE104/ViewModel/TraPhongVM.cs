using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace QuanLyKhachSan_SE104.ViewModel
{
    public class TraPhongVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<ChiTietDichVu> _listDichVuSuDung;
        public ObservableCollection<ChiTietDichVu> ListDichVuSuDung
        {
            get => _listDichVuSuDung;
            set { _listDichVuSuDung = value; OnPropertyChanged(); }
        }

        private List<KhachHang> _listKhachHang;
        public List<KhachHang> ListKhachHang
        {
            get => _listKhachHang;
            set { _listKhachHang = value; OnPropertyChanged(); }
        }

        private KhachHang _selectedKhachHang;
        public KhachHang SelectedKhachHang
        {
            get => _selectedKhachHang;
            set
            {
                _selectedKhachHang = value;
                OnPropertyChanged();
                LoadChiTietThanhToan();
            }
        }

        private decimal _tongTienThanhToan;
        public decimal TongTienThanhToan
        {
            get => _tongTienThanhToan;
            set { _tongTienThanhToan = value; OnPropertyChanged(); }
        }

        public string CurrentTime => DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // ── Hóa đơn popup ──
        private bool _isHoaDonOpen;
        public bool IsHoaDonOpen
        {
            get => _isHoaDonOpen;
            set { _isHoaDonOpen = value; OnPropertyChanged(); }
        }

        private string _hoaDonTenKhach;
        public string HoaDonTenKhach
        {
            get => _hoaDonTenKhach;
            set { _hoaDonTenKhach = value; OnPropertyChanged(); }
        }

        private string _hoaDonSoPhong;
        public string HoaDonSoPhong
        {
            get => _hoaDonSoPhong;
            set { _hoaDonSoPhong = value; OnPropertyChanged(); }
        }

        private string _hoaDonThoiGian;
        public string HoaDonThoiGian
        {
            get => _hoaDonThoiGian;
            set { _hoaDonThoiGian = value; OnPropertyChanged(); }
        }

        private decimal _hoaDonTienPhong;
        public decimal HoaDonTienPhong
        {
            get => _hoaDonTienPhong;
            set { _hoaDonTienPhong = value; OnPropertyChanged(); }
        }

        private decimal _hoaDonTienDichVu;
        public decimal HoaDonTienDichVu
        {
            get => _hoaDonTienDichVu;
            set { _hoaDonTienDichVu = value; OnPropertyChanged(); }
        }

        private decimal _hoaDonTongTien;
        public decimal HoaDonTongTien
        {
            get => _hoaDonTongTien;
            set { _hoaDonTongTien = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmCheckoutCommand { get; set; }
        public ICommand CloseHoaDonCommand { get; set; }

        public TraPhongVM()
        {
            ListDichVuSuDung = new ObservableCollection<ChiTietDichVu>();
            LoadData();
            ConfirmCheckoutCommand = new RelayCommand<object>((p) => { ExecuteCheckout(); });
            CloseHoaDonCommand = new RelayCommand<object>((p) => { IsHoaDonOpen = false; });
        }

        void LoadData()
        {
            try
            {
                using (var db = new QuanLyKhachSanContext())
                {
                    var khachHangs = db.DatPhongs
                                       .Include(d => d.KhachHang)
                                       .Where(d => d.TrangThaiDat == 2)
                                       .Select(d => d.KhachHang)
                                       .Distinct()
                                       .ToList();
                    ListKhachHang = khachHangs;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        void LoadChiTietThanhToan()
        {
            if (SelectedKhachHang == null) return;

            using (var db = new QuanLyKhachSanContext())
            {
                var datPhong = db.DatPhongs
                                 .Include(d => d.ChiTietDatPhongs)
                                    .ThenInclude(c => c.ChiTietDichVus)
                                    .ThenInclude(cdv => cdv.DichVu)
                                 .FirstOrDefault(d => d.MaKhachHang == SelectedKhachHang.MaKhachHang && d.TrangThaiDat == 2);

                if (datPhong != null)
                {
                    var dsDichVu = new List<ChiTietDichVu>();
                    decimal tongTienDichVu = 0;
                    decimal tongTienPhong = 0;

                    foreach (var ctdp in datPhong.ChiTietDatPhongs)
                    {
                        // Tính tiền phòng theo số giờ (làm tròn lên)
                        var soGio = (decimal)Math.Ceiling((DateTime.Now - ctdp.NgayCheckIn).TotalHours);
                        if (soGio < 1) soGio = 1;
                        tongTienPhong += soGio * ctdp.GiaDat;

                        foreach (var dv in ctdp.ChiTietDichVus)
                        {
                            dsDichVu.Add(dv);
                            tongTienDichVu += (dv.SoLuong * dv.DonGia);
                        }
                    }

                    ListDichVuSuDung = new ObservableCollection<ChiTietDichVu>(dsDichVu);
                    TongTienThanhToan = tongTienPhong + tongTienDichVu - datPhong.TienCoc;
                }
            }
        }

        void ExecuteCheckout()
        {
            if (SelectedKhachHang == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng!");
                return;
            }

            using (var db = new QuanLyKhachSanContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var datPhong = db.DatPhongs
                                         .Include(d => d.ChiTietDatPhongs)
                                            .ThenInclude(c => c.Phong)
                                         .Include(d => d.ChiTietDatPhongs)
                                            .ThenInclude(c => c.ChiTietDichVus)
                                         .FirstOrDefault(d => d.MaKhachHang == SelectedKhachHang.MaKhachHang && d.TrangThaiDat == 2);

                        if (datPhong == null)
                        {
                            MessageBox.Show("Không tìm thấy thông tin phòng!");
                            return;
                        }

                        // ── Tính tiền phòng theo giờ, tiền dịch vụ ──
                        decimal tongTienPhong = 0;
                        decimal tongTienDichVu = 0;
                        string soPhongs = "";
                        foreach (var ctdp in datPhong.ChiTietDatPhongs)
                        {
                            var soGio = (decimal)Math.Ceiling((DateTime.Now - ctdp.NgayCheckIn).TotalHours);
                            if (soGio < 1) soGio = 1;
                            tongTienPhong += soGio * ctdp.GiaDat;

                            if (ctdp.Phong != null)
                                soPhongs += (soPhongs.Length == 0 ? "" : ", ") + ctdp.Phong.TenPhong;

                            foreach (var dv in ctdp.ChiTietDichVus)
                                tongTienDichVu += dv.SoLuong * dv.DonGia;
                        }

                        var hoaDon = new HoaDon
                        {
                            MaDatPhong = datPhong.MaDatPhong,
                            MaNhanVien = 1,
                            TongTienPhong = tongTienPhong,
                            TongTienDichVu = tongTienDichVu,
                            PhuPhi = 0,
                            TienCoc = datPhong.TienCoc,
                            TongThanhToan = TongTienThanhToan,
                            NgayThanhToan = DateTime.Now,
                            PhuongThucThanhToan = 0,
                            TrangThaiThanhToan = "Đã thanh toán",
                            GhiChu = "Check-out"
                        };
                        db.HoaDons.Add(hoaDon);

                        foreach (var ctdp in datPhong.ChiTietDatPhongs)
                        {
                            ctdp.NgayCheckOut = DateTime.Now;

                            var phong = db.Phongs.Find(ctdp.MaPhong);
                            if (phong != null)
                            {
                                phong.TrangThaiThue = 0;
                                phong.TrangThaiDonDep = 2;
                            }
                        }

                        datPhong.TrangThaiDat = 3;

                        db.SaveChanges();
                        transaction.Commit();

                        // ── Hiển thị popup hóa đơn ──
                        HoaDonTenKhach = SelectedKhachHang.HoTen;
                        HoaDonSoPhong = soPhongs;
                        HoaDonThoiGian = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        HoaDonTienPhong = tongTienPhong;
                        HoaDonTienDichVu = tongTienDichVu;
                        HoaDonTongTien = TongTienThanhToan;
                        IsHoaDonOpen = true;

                        SelectedKhachHang = null;
                        ListDichVuSuDung.Clear();
                        TongTienThanhToan = 0;
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}

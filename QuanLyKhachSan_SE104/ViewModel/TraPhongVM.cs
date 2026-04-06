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

        public ICommand ConfirmCheckoutCommand { get; set; }

        public TraPhongVM()
        {
            ListDichVuSuDung = new ObservableCollection<ChiTietDichVu>();
            LoadData();
            ConfirmCheckoutCommand = new RelayCommand<object>((p) => { ExecuteCheckout(); });
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
                        tongTienPhong += ctdp.GiaDat;

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
                                         .FirstOrDefault(d => d.MaKhachHang == SelectedKhachHang.MaKhachHang && d.TrangThaiDat == 2);

                        if (datPhong == null)
                        {
                            MessageBox.Show("Không tìm thấy thông tin phòng!");
                            return;
                        }

                        var hoaDon = new HoaDon
                        {
                            MaDatPhong = datPhong.MaDatPhong,
                            MaNhanVien = 1,
                            TongTienPhong = 0,
                            TongTienDichVu = 0,
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
                        MessageBox.Show("Trả phòng thành công!");

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
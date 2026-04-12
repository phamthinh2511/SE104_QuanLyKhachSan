using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.View.ChiTietDatPhong;
using QuanLyKhachSan_SE104.View.DatPhong;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

// Alias tránh conflict tên DatPhong (namespace vs Model)
using ModelDatPhong = QuanLyKhachSan_SE104.Model.DatPhong;

namespace QuanLyKhachSan_SE104.ViewModel.DatPhong
{
    public class DatPhongListViewModel : INotifyPropertyChanged
    {
        // ── Data ──────────────────────────────────────────
        private List<ModelDatPhong> _allDatPhong;

        private ObservableCollection<ModelDatPhong> _listDatPhong;
        public ObservableCollection<ModelDatPhong> ListDatPhong
        {
            get => _listDatPhong;
            set { _listDatPhong = value; OnPropertyChanged(); }
        }

        // ── Search ────────────────────────────────────────
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ExecuteSearch(); }
        }

        // ── Commands ──────────────────────────────────────
        public ICommand XemChiTietCommand { get; }
        public ICommand XoaCommand { get; }
        public ICommand DatPhongCommand { get; }

        public DatPhongListViewModel()
        {
            LoadData();

            // Mở popup chi tiết phiếu thuê
            XemChiTietCommand = new RelayCommand<ModelDatPhong>(dp =>
            {
                if (dp == null) return;
                var win = new ChiTietDatPhongWindow(dp);
                win.ShowDialog();
            });

            // Xóa phiếu thuê
            XoaCommand = new RelayCommand<ModelDatPhong>(dp =>
            {
                if (dp == null) return;
                var result = MessageBox.Show(
                    $"Xác nhận xóa phiếu thuê #{dp.MaDatPhong}?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _allDatPhong.Remove(dp);
                    ExecuteSearch();
                }
            });

            // Mở form đặt phòng mới
            DatPhongCommand = new RelayCommand<object>(_ =>
            {
                var win = new Window
                {
                    Title = "Đặt Phòng",
                    Height = 700,
                    Width = 1100,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Content = new DatPhongPage() 
                };
                win.ShowDialog();
            });
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ListDatPhong = new ObservableCollection<ModelDatPhong>(_allDatPhong);
                return;
            }

            var lower = SearchText.Trim().ToLower();
            ListDatPhong = new ObservableCollection<ModelDatPhong>(
                _allDatPhong.Where(dp =>
                    dp.KhachHang?.HoTen?.ToLower().Contains(lower) == true)
            );
        }

        private void LoadData()
        {
            // TODO: Thay bằng load từ DB
            _allDatPhong = new List<ModelDatPhong>
            {
                new ModelDatPhong
                {
                    MaDatPhong = 20, NgayDat = new DateTime(2021,10,29),
                    KhachHang = new KhachHang { HoTen = "Mai Thị G" },
                    NhanVien  = new NhanVien  { HoTen = "Nguyễn Văn Duy" },
                    ChiTietDatPhongs = new List<ChiTietDatPhong>
                    {
                        new ChiTietDatPhong { MaPhong=101, NgayCheckIn=new DateTime(2021,11,1), NgayCheckOut=new DateTime(2021,11,3) }
                    }
                },
                new ModelDatPhong
                {
                    MaDatPhong = 21, NgayDat = new DateTime(2021,10,29),
                    KhachHang = new KhachHang { HoTen = "Nguyễn Việt Quang" },
                    NhanVien  = new NhanVien  { HoTen = "Nguyễn Văn Duy" },
                    ChiTietDatPhongs = new List<ChiTietDatPhong>()
                },
                new ModelDatPhong
                {
                    MaDatPhong = 22, NgayDat = new DateTime(2021,11,1),
                    KhachHang = new KhachHang { HoTen = "Trần Hoàng Gia" },
                    NhanVien  = new NhanVien  { HoTen = "Nguyễn Văn Duy" },
                    ChiTietDatPhongs = new List<ChiTietDatPhong>
                    {
                        new ChiTietDatPhong { MaPhong=102, NgayCheckIn=new DateTime(2021,11,1), NgayCheckOut=new DateTime(2021,11,5) }
                    }
                },
            };

            ListDatPhong = new ObservableCollection<ModelDatPhong>(_allDatPhong);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
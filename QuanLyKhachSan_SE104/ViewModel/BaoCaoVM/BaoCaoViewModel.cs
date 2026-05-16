using LiveCharts;
using LiveCharts.Wpf;
using QuanLyKhachSan_SE104.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.ViewModel.BaoCaoVM
{
    public class BaoCaoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private int _thangSelected;
        public int ThangSelected { get => _thangSelected; set { _thangSelected = value; OnPropertyChanged(); } }

        private int _namSelected;
        public int NamSelected { get => _namSelected; set { _namSelected = value; OnPropertyChanged(); } }

        private string _tieuChiSelected;
        public string TieuChiSelected { get => _tieuChiSelected; set { _tieuChiSelected = value; OnPropertyChanged(); } }

        public ObservableCollection<int> DanhSachThang { get; set; }
        public ObservableCollection<int> DanhSachNam { get; set; }
        public ObservableCollection<string> DanhSachTieuChi { get; set; }

        private decimal _tongDoanhThu;
        public decimal TongDoanhThu { get => _tongDoanhThu; set { _tongDoanhThu = value; OnPropertyChanged(); } }

        private ObservableCollection<BaoCaoDoanhThuModel> _danhSachBaoCao;
        public ObservableCollection<BaoCaoDoanhThuModel> DanhSachBaoCao { get => _danhSachBaoCao; set { _danhSachBaoCao = value; OnPropertyChanged(); } }

        private SeriesCollection _chartSeries;
        public SeriesCollection ChartSeries { get => _chartSeries; set { _chartSeries = value; OnPropertyChanged(); } }

        private string[] _chartLabels;
        public string[] ChartLabels { get => _chartLabels; set { _chartLabels = value; OnPropertyChanged(); } }

        public ICommand ThongKeCommand { get; }

        public BaoCaoViewModel()
        {
            DanhSachThang = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            DanhSachNam = new ObservableCollection<int> { 2024, 2025, 2026 };
            DanhSachTieuChi = new ObservableCollection<string> { "Theo loại phòng", "Theo dịch vụ" };

            ThangSelected = DateTime.Now.Month;
            NamSelected = DateTime.Now.Year;
            TieuChiSelected = DanhSachTieuChi[0];

            ThongKeCommand = new RelayCommand<object>(p => ThucHienThongKe());
            ThucHienThongKe();
        }

        private void ThucHienThongKe()
        {
            // Dữ liệu giả lập ban đầu để hiển thị giao diện
            TongDoanhThu = 185000000;

            ChartSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<decimal> { 80000000, 65000000, 40000000 }
                }
            };
            ChartLabels = new[] { "Phòng VIP", "Phòng Deluxe", "Phòng Standard" };

            DanhSachBaoCao = new ObservableCollection<BaoCaoDoanhThuModel>
            {
                new BaoCaoDoanhThuModel { STT = 1, MaGiaoDich = "HD0001", TenLoai = "Phòng 101 (Standard)", NgayTao = DateTime.Today, DoanhThu = 1500000 },
                new BaoCaoDoanhThuModel { STT = 2, MaGiaoDich = "HD0002", TenLoai = "Phòng 302 (Deluxe)", NgayTao = DateTime.Today.AddDays(-1), DoanhThu = 2400000 }
            };
        }
    }

    public class BaoCaoDoanhThuModel
    {
        public int STT { get; set; }
        public string MaGiaoDich { get; set; }
        public string TenLoai { get; set; }
        public DateTime NgayTao { get; set; }
        public decimal DoanhThu { get; set; }
    }
}
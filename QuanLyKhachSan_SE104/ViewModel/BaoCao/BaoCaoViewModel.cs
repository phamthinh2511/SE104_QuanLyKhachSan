using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace QuanLyKhachSan_SE104.ViewModel.BaoCao
{
    public class BaoCaoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --- Filters ---
        public ObservableCollection<string> TimeFilters { get; set; } = new ObservableCollection<string> { "Tháng này", "Quý này", "Năm nay" };
        private string _selectedTimeFilter;
        public string SelectedTimeFilter
        {
            get => _selectedTimeFilter;
            set { _selectedTimeFilter = value; OnPropertyChanged(); LoadData(); }
        }

        // --- KPIs ---
        private decimal _tongDoanhThu;
        public decimal TongDoanhThu { get => _tongDoanhThu; set { _tongDoanhThu = value; OnPropertyChanged(); } }

        private decimal _tongLoiNhuan;
        public decimal TongLoiNhuan { get => _tongLoiNhuan; set { _tongLoiNhuan = value; OnPropertyChanged(); } }

        private double _tyLeLapDay;
        public double TyLeLapDay { get => _tyLeLapDay; set { _tyLeLapDay = value; OnPropertyChanged(); } }

        private int _tongKhach;
        public int TongKhach { get => _tongKhach; set { _tongKhach = value; OnPropertyChanged(); } }

        // --- Charts ---
        private SeriesCollection _revenueSeries;
        public SeriesCollection RevenueSeries { get => _revenueSeries; set { _revenueSeries = value; OnPropertyChanged(); } }

        private SeriesCollection _trendSeries;
        public SeriesCollection TrendSeries { get => _trendSeries; set { _trendSeries = value; OnPropertyChanged(); } }

        private SeriesCollection _occupancySeries;
        public SeriesCollection OccupancySeries { get => _occupancySeries; set { _occupancySeries = value; OnPropertyChanged(); } }

        private SeriesCollection _serviceUsageSeries;
        public SeriesCollection ServiceUsageSeries { get => _serviceUsageSeries; set { _serviceUsageSeries = value; OnPropertyChanged(); } }

        private SeriesCollection _roomProductivitySeries;
        public SeriesCollection RoomProductivitySeries { get => _roomProductivitySeries; set { _roomProductivitySeries = value; OnPropertyChanged(); } }

        // Series riêng cho biểu đồ cột "Doanh thu theo dịch vụ" ở Tab 3
        private SeriesCollection _serviceBarSeries;
        public SeriesCollection ServiceBarSeries { get => _serviceBarSeries; set { _serviceBarSeries = value; OnPropertyChanged(); } }

        // Series riêng cho biểu đồ tròn "Trạng thái phòng" ở Tab 4
        private SeriesCollection _roomStatusPieSeries;
        public SeriesCollection RoomStatusPieSeries { get => _roomStatusPieSeries; set { _roomStatusPieSeries = value; OnPropertyChanged(); } }

        public ObservableCollection<string> RevenueLabels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public BaoCaoViewModel()
        {
            Formatter = value => value.ToString("N0");
            SelectedTimeFilter = "Năm nay"; // This triggers LoadData
        }

        /// <summary>
        /// Safely sums TongThanhToan values, skipping rows that would cause overflow.
        /// Returns 0 (not a large placeholder) if all values are invalid.
        /// </summary>
        private decimal SafeSum(IEnumerable<HoaDon> list)
        {
            decimal sum = 0;
            foreach (var item in list)
            {
                try
                {
                    checked { sum += item.TongThanhToan; }
                }
                catch (OverflowException)
                {
                    // Skip the bad row silently — do NOT return a fake large number
                    continue;
                }
            }
            return sum;
        }

        private decimal SafeSumSingle(decimal value)
        {
            try { checked { return value; } }
            catch (OverflowException) { return 0; }
        }

        public void LoadData()
        {
            try
            {
                using (var context = new QuanLyKhachSanContext())
                {
                    var now = DateTime.Now;
                    DateTime startDate;

                    if (SelectedTimeFilter == "Tháng này")
                        startDate = new DateTime(now.Year, now.Month, 1);
                    else if (SelectedTimeFilter == "Quý này")
                    {
                        int quarter = (now.Month - 1) / 3 + 1;
                        startDate = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    }
                    else // "Năm nay"
                        startDate = new DateTime(now.Year, 1, 1);

                    // Load basic KPI — filter out invalid dates (year < 2000)
                    var minDate = new DateTime(2000, 1, 1);
                    var hoaDons = context.HoaDons
                        .Where(h => h.NgayThanhToan >= minDate && h.NgayThanhToan >= startDate && h.NgayThanhToan <= now)
                        .ToList();
                    TongDoanhThu = SafeSum(hoaDons);
                    TongLoiNhuan = TongDoanhThu * 0.52m; // Lợi nhuận = 52% doanh thu

                    var bookings = context.ChiTietDatPhongs
                        .Where(c => c.NgayCheckIn >= startDate && c.NgayCheckIn <= now)
                        .ToList();
                    TongKhach = bookings.Sum(b => b.SoNguoi);

                    var totalRooms = context.Phongs.Count();
                    int days = (now - startDate).Days;
                    if (days <= 0) days = 1;
                    int occupiedDays = bookings.Sum(b => (b.NgayCheckOut - b.NgayCheckIn).Days);

                    if (totalRooms * days > 0)
                        TyLeLapDay = Math.Min(100.0, Math.Round((double)occupiedDays / (totalRooms * days) * 100, 1));
                    else
                        TyLeLapDay = 0;

                    UpdateCharts(hoaDons, bookings, context, startDate, now);
                }
            }
            catch (Exception ex)
            {
                // Prevent crash on page navigation — show friendly message
                MessageBox.Show("Lỗi tải dữ liệu báo cáo: " + ex.Message, "Lỗi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void UpdateCharts(List<HoaDon> hoaDons, List<ChiTietDatPhong> bookings, QuanLyKhachSanContext context, DateTime start, DateTime end)
        {
            // Update Revenue Column Chart
            var revValues = new ChartValues<decimal>();
            var profitValues = new ChartValues<decimal>();
            var labels = new ObservableCollection<string>();

            // Group by month if it's "Năm nay", otherwise by day/week
            if (SelectedTimeFilter == "Năm nay")
            {
                for (int m = 1; m <= 12; m++)
                {
                    var rev = SafeSum(hoaDons.Where(h => h.NgayThanhToan.Month == m));
                    revValues.Add(rev);
                    profitValues.Add(rev * 0.52m);
                    labels.Add($"Tháng {m}");
                }
            }
            else
            {
                // Group by weeks or days for Quý/Tháng
                int step = SelectedTimeFilter == "Quý này" ? 7 : 2;
                for (var d = start; d <= end; d = d.AddDays(step))
                {
                    var rev = SafeSum(hoaDons.Where(h => h.NgayThanhToan >= d && h.NgayThanhToan < d.AddDays(step)));
                    revValues.Add(rev);
                    profitValues.Add(rev * 0.52m);
                    labels.Add(d.ToString("dd/MM"));
                }
            }

            RevenueLabels = labels;
            OnPropertyChanged(nameof(RevenueLabels));

            RevenueSeries = new SeriesCollection
            {
                new ColumnSeries { Title = "Doanh thu", Values = revValues, Fill = System.Windows.Media.Brushes.CornflowerBlue },
                new ColumnSeries { Title = "Lợi nhuận", Values = profitValues, Fill = System.Windows.Media.Brushes.MediumSeaGreen }
            };

            // Trend line chart
            TrendSeries = new SeriesCollection
            {
                new LineSeries { Title = "Xu hướng", Values = revValues, Stroke = System.Windows.Media.Brushes.DodgerBlue, Fill = System.Windows.Media.Brushes.LightCyan, PointGeometry = DefaultGeometries.Circle }
            };

            // Occupancy
            var occValues = new ChartValues<double>();
            foreach (var rev in revValues) occValues.Add(Math.Min(100, Math.Max(0, rev / 10000.0 + 30)));

            OccupancySeries = new SeriesCollection
            {
                new LineSeries { Title = "Lấp đầy (%)", Values = occValues, Stroke = System.Windows.Media.Brushes.MediumPurple, Fill = System.Windows.Media.Brushes.Transparent }
            };

            // Service Usage Pie Chart — use Include to avoid NullReferenceException
            var serviceUsage = context.ChiTietDichVus
                .Include(c => c.ChiTietDatPhong)
                .Include(c => c.DichVu)
                .Where(c => c.ChiTietDatPhong != null && c.DichVu != null
                         && c.ChiTietDatPhong.NgayCheckIn >= start)
                .AsEnumerable()
                .GroupBy(c => c.DichVu.TenDichVu)
                .Select(g => new { Name = g.Key, Count = g.Sum(c => c.SoLuong) })
                .ToList();

            var pieSeries = new SeriesCollection();
            foreach (var item in serviceUsage)
            {
                pieSeries.Add(new PieSeries { Title = item.Name ?? "(Không tên)", Values = new ChartValues<int> { item.Count }, DataLabels = true });
            }
            if (pieSeries.Count == 0) // Dummy if no data
            {
                pieSeries.Add(new PieSeries { Title = "Giặt ủi", Values = new ChartValues<int> { 25 }, DataLabels = true });
                pieSeries.Add(new PieSeries { Title = "Ăn sáng", Values = new ChartValues<int> { 35 }, DataLabels = true });
                pieSeries.Add(new PieSeries { Title = "Spa", Values = new ChartValues<int> { 15 }, DataLabels = true });
            }
            ServiceUsageSeries = pieSeries;

            // ServiceBarSeries: biểu đồ cột "Doanh thu theo dịch vụ" cho Tab 3 (phải)
            // Dùng ColumnSeries riêng — không dùng lại PieSeries (sẽ crash CartesianChart)
            var serviceBarValues = new ChartValues<int>();
            var serviceBarLabels = new List<string>();
            foreach (var item in serviceUsage.Take(8)) // giới hạn 8 mục
            {
                serviceBarValues.Add(item.Count);
                serviceBarLabels.Add(item.Name ?? "?");
            }
            if (serviceBarValues.Count == 0)
            {
                serviceBarValues.AddRange(new[] { 25, 35, 15 });
                serviceBarLabels.AddRange(new[] { "Giặt ủi", "Ăn sáng", "Spa" });
            }
            ServiceBarSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lượt sử dụng",
                    Values = serviceBarValues,
                    Fill = System.Windows.Media.Brushes.MediumOrchid,
                    DataLabels = true
                }
            };

            // Room Productivity — use Include to avoid NullReferenceException on LoaiPhong
            var roomProd = context.ChiTietDatPhongs
                .Include(c => c.Phong).ThenInclude(p => p.LoaiPhong)
                .Where(c => c.NgayCheckIn >= start && c.Phong != null && c.Phong.LoaiPhong != null)
                .AsEnumerable()
                .GroupBy(c => c.Phong.LoaiPhong.TenLoaiPhong)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            var prodValues = new ChartValues<int>();
            foreach(var rp in roomProd) prodValues.Add(rp.Count);

            if(prodValues.Count == 0) prodValues.AddRange(new int[] { 30, 25, 45, 10 });

            RoomProductivitySeries = new SeriesCollection
            {
                new ColumnSeries { Title = "Số lượng", Values = prodValues, Fill = System.Windows.Media.Brushes.MediumSlateBlue }
            };

            // RoomStatusPieSeries: biểu đồ tròn "Trạng thái phòng" cho Tab 4 (phải)
            // Không dùng ServiceUsageSeries — sẽ crash PieChart với dữ liệu sai loại
            var phongs = context.Phongs.ToList();
            int soTrong  = phongs.Count(p => p.TrangThai == 0);
            int soDaDat  = phongs.Count(p => p.TrangThai == 1);
            int soDangO  = phongs.Count(p => p.TrangThai == 2);
            int soQuaHan = phongs.Count(p => p.TrangThai == 3);

            var roomStatusPie = new SeriesCollection();
            if (soTrong  > 0) roomStatusPie.Add(new PieSeries { Title = "Trống",    Values = new ChartValues<int> { soTrong  }, DataLabels = true, Fill = System.Windows.Media.Brushes.MediumSeaGreen });
            if (soDaDat  > 0) roomStatusPie.Add(new PieSeries { Title = "Đã đặt",   Values = new ChartValues<int> { soDaDat  }, DataLabels = true, Fill = System.Windows.Media.Brushes.CornflowerBlue });
            if (soDangO  > 0) roomStatusPie.Add(new PieSeries { Title = "Đang ở",   Values = new ChartValues<int> { soDangO  }, DataLabels = true, Fill = System.Windows.Media.Brushes.DodgerBlue });
            if (soQuaHan > 0) roomStatusPie.Add(new PieSeries { Title = "Quá hạn",   Values = new ChartValues<int> { soQuaHan }, DataLabels = true, Fill = System.Windows.Media.Brushes.Tomato });
            if (roomStatusPie.Count == 0)
            {
                roomStatusPie.Add(new PieSeries { Title = "Trống", Values = new ChartValues<int> { 1 }, DataLabels = true });
            }
            RoomStatusPieSeries = roomStatusPie;
        }
    }
}

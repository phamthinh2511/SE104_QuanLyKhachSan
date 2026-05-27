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

        private SeriesCollection _guestSeries;
        public SeriesCollection GuestSeries { get => _guestSeries; set { _guestSeries = value; OnPropertyChanged(); } }

        private ObservableCollection<string> _roomProductivityLabels;
        public ObservableCollection<string> RoomProductivityLabels { get => _roomProductivityLabels; set { _roomProductivityLabels = value; OnPropertyChanged(); } }

        private ObservableCollection<string> _serviceBarLabels;
        public ObservableCollection<string> ServiceBarLabels { get => _serviceBarLabels; set { _serviceBarLabels = value; OnPropertyChanged(); } }

        public ObservableCollection<string> RevenueLabels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public BaoCaoViewModel()
        {
            Formatter = value => value.ToString("N0");
            SelectedTimeFilter = "Năm nay"; // This triggers LoadData
        }

        /// <summary>
        /// Safely sums correct invoice revenue values, skipping rows that would cause overflow.
        /// Returns 0 if all values are invalid.
        /// </summary>
        private decimal SafeSum(IEnumerable<HoaDon> list)
        {
            decimal sum = 0;
            foreach (var item in list)
            {
                try
                {
                    decimal revenue = item.TongTienPhong + item.TongTienDichVu + item.PhuPhi;
                    if (revenue == 0 && item.TongThanhToan > 0)
                    {
                        revenue = item.TongThanhToan + item.TienCoc;
                    }
                    checked { sum += revenue; }
                }
                catch (OverflowException)
                {
                    // Skip the bad row silently — do NOT return a fake large number
                    continue;
                }
            }
            return sum;
        }

        // Giá trị tối đa hợp lệ cho một hóa đơn (10 tỷ VND — bất kỳ khách sạn thực tế nào cũng không vượt quá mức này)
        private const decimal MAX_VALID_HOA_DON = 10_000_000_000m; // 10 tỷ VND

        private decimal SafeSumSingle(decimal value)
        {
            try { checked { return value; } }
            catch (OverflowException) { return 0; }
        }

        private int GetOverlapDays(DateTime checkIn, DateTime checkOut, DateTime rangeStart, DateTime rangeEnd)
        {
            DateTime start = checkIn.Date;
            DateTime end = checkOut.Date;
            DateTime rStart = rangeStart.Date;
            DateTime rEnd = rangeEnd.Date;

            if (start > rEnd || end < rStart)
                return 0;

            if (start == end)
            {
                // Same day check-in/check-out
                return (start >= rStart && start <= rEnd) ? 1 : 0;
            }

            // Normal case: check-out > check-in
            // The nights occupied are [start, end - 1]
            DateTime overlapStart = start > rStart ? start : rStart;
            DateTime overlapEnd = (end.AddDays(-1) < rEnd) ? end.AddDays(-1) : rEnd;

            if (overlapStart <= overlapEnd)
            {
                return (overlapEnd - overlapStart).Days + 1;
            }
            return 0;
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

                    // Load basic KPI — filter out invalid dates (year < 2000), data test bất thường, và chỉ lấy hóa đơn đã thanh toán
                    var minDate = new DateTime(2000, 1, 1);
                    var hoaDons = context.HoaDons
                        .Where(h => h.NgayThanhToan >= minDate
                                 && h.NgayThanhToan >= startDate
                                 && h.NgayThanhToan <= now
                                 && h.TrangThaiThanhToan == "Đã thanh toán"
                                 && h.TongThanhToan >= 0
                                 && h.TongThanhToan <= MAX_VALID_HOA_DON) // loại bỏ data test / giá trị bất thường
                        .ToList();
                    if (hoaDons.Count == 0)
                    {
                        // Kiểm tra xem có data nhưng bị lọc không — cảnh báo nếu cần
                        var allHoaDonCount = context.HoaDons.Count(h => h.NgayThanhToan >= startDate && h.NgayThanhToan <= now);
                        if (allHoaDonCount > 0)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[BaoCao] Có {allHoaDonCount} hóa đơn trong kỳ nhưng bị loại do TongThanhToan > {MAX_VALID_HOA_DON:#,0} đồng.");
                        }
                    }
                    TongDoanhThu = SafeSum(hoaDons);
                    TongLoiNhuan = TongDoanhThu * 0.52m; // Lợi nhuận = 52% doanh thu

                    // Load bookings that overlap with [startDate, now]
                    var bookings = context.ChiTietDatPhongs
                        .Include(c => c.DatPhong)
                        .Where(c => c.NgayCheckIn <= now 
                                 && c.NgayCheckOut >= startDate
                                 && (c.DatPhong.TrangThaiDat == 2 || c.DatPhong.TrangThaiDat == 3))
                        .ToList();
                    TongKhach = bookings.Sum(b => b.SoNguoi);

                    var totalRooms = context.Phongs.Count(p => !p.IsDeleted);
                    int days = (now.Date - startDate.Date).Days + 1;
                    if (days <= 0) days = 1;
                    int occupiedDays = bookings.Sum(b => GetOverlapDays(b.NgayCheckIn, b.NgayCheckOut, startDate, now));

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
            var labels = new ObservableCollection<string>();

            // For occupancy chart
            var occValues = new ChartValues<double>();
            int totalRooms = context.Phongs.Count(p => !p.IsDeleted);

            // For guest chart
            var guestValues = new ChartValues<int>();

            // Group by month if it's "Năm nay", otherwise by day/week
            if (SelectedTimeFilter == "Năm nay")
            {
                for (int m = 1; m <= 12; m++)
                {
                    var rev = SafeSum(hoaDons.Where(h => h.NgayThanhToan.Month == m));
                    revValues.Add(rev);
                    labels.Add($"Tháng {m}");

                    // Compute real occupancy for month m
                    var monthStart = new DateTime(start.Year, m, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    var bookingsInMonth = bookings
                        .Where(b => b.NgayCheckIn.Date <= monthEnd.Date && b.NgayCheckOut.Date >= monthStart.Date)
                        .ToList();
                    int occDays = bookingsInMonth.Sum(b => GetOverlapDays(b.NgayCheckIn, b.NgayCheckOut, monthStart, monthEnd));
                    int daysInMonth = (monthEnd.Date - monthStart.Date).Days + 1;
                    double monthOcc = 0;
                    if (totalRooms * daysInMonth > 0)
                        monthOcc = Math.Min(100.0, Math.Round((double)occDays / (totalRooms * daysInMonth) * 100, 1));
                    occValues.Add(monthOcc);

                    // Compute real guest count for month m
                    int guestsInMonth = bookingsInMonth.Sum(b => b.SoNguoi);
                    guestValues.Add(guestsInMonth);
                }
            }
            else
            {
                // Group by weeks or days for Quý/Tháng
                int step = SelectedTimeFilter == "Quý này" ? 7 : 2;
                for (var d = start; d <= end; d = d.AddDays(step))
                {
                    var stepStart = d;
                    var stepEnd = d.AddDays(step - 1);
                    if (stepEnd > end) stepEnd = end;

                    var rev = SafeSum(hoaDons.Where(h => h.NgayThanhToan >= stepStart && h.NgayThanhToan < d.AddDays(step)));
                    revValues.Add(rev);
                    labels.Add(d.ToString("dd/MM"));

                    // Compute real occupancy for this step
                    var bookingsInStep = bookings
                        .Where(b => b.NgayCheckIn.Date <= stepEnd.Date && b.NgayCheckOut.Date >= stepStart.Date)
                        .ToList();
                    int occDays = bookingsInStep.Sum(b => GetOverlapDays(b.NgayCheckIn, b.NgayCheckOut, stepStart, stepEnd));
                    int daysInStep = (stepEnd.Date - stepStart.Date).Days + 1;
                    double stepOcc = 0;
                    if (totalRooms * daysInStep > 0)
                        stepOcc = Math.Min(100.0, Math.Round((double)occDays / (totalRooms * daysInStep) * 100, 1));
                    occValues.Add(stepOcc);

                    // Compute real guest count for this step
                    int guestsInStep = bookingsInStep.Sum(b => b.SoNguoi);
                    guestValues.Add(guestsInStep);
                }
            }

            RevenueLabels = labels;
            OnPropertyChanged(nameof(RevenueLabels));

            RevenueSeries = new SeriesCollection
            {
                new ColumnSeries { Title = "Doanh thu", Values = revValues, Fill = System.Windows.Media.Brushes.CornflowerBlue }
            };

            // Trend line chart
            TrendSeries = new SeriesCollection
            {
                new LineSeries { Title = "Xu hướng", Values = revValues, Stroke = System.Windows.Media.Brushes.DodgerBlue, Fill = System.Windows.Media.Brushes.LightCyan, PointGeometry = DefaultGeometries.Circle }
            };

            // Occupancy
            OccupancySeries = new SeriesCollection
            {
                new LineSeries { Title = "Lấp đầy (%)", Values = occValues, Stroke = System.Windows.Media.Brushes.MediumPurple, Fill = System.Windows.Media.Brushes.Transparent }
            };

            // Guest Series
            GuestSeries = new SeriesCollection
            {
                new ColumnSeries { Title = "Lượng khách", Values = guestValues, Fill = System.Windows.Media.Brushes.Orange }
            };

            // Service Usage Pie Chart — filter null navigation props AFTER AsEnumerable
            var serviceUsage = context.ChiTietDichVus
                .Include(c => c.DichVu)
                .Where(c => c.ThoiGianSuDung >= start && c.ThoiGianSuDung <= end && c.DichVu != null)
                .AsEnumerable()
                .GroupBy(c => c.DichVu.TenDichVu ?? "(Không tên)")
                .Select(g => new { 
                    Name = g.Key, 
                    Count = g.Sum(c => c.SoLuong),
                    Revenue = g.Sum(c => c.SoLuong * c.DonGia)
                })
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
            var serviceBarValues = new ChartValues<decimal>();
            var serviceBarLabelsList = new ObservableCollection<string>();
            foreach (var item in serviceUsage.Take(8)) // giới hạn 8 mục
            {
                serviceBarValues.Add(item.Revenue);
                serviceBarLabelsList.Add(item.Name ?? "?");
            }
            if (serviceBarValues.Count == 0)
            {
                serviceBarValues.AddRange(new decimal[] { 250000m, 350000m, 150000m });
                serviceBarLabelsList.Add("Giặt ủi");
                serviceBarLabelsList.Add("Ăn sáng");
                serviceBarLabelsList.Add("Spa");
            }
            ServiceBarLabels = serviceBarLabelsList;

            ServiceBarSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu (₫)",
                    Values = serviceBarValues,
                    Fill = System.Windows.Media.Brushes.MediumOrchid,
                    DataLabels = true
                }
            };

            // Room Productivity
            var roomProd = context.ChiTietDatPhongs
                .Include(c => c.DatPhong)
                .Include(c => c.Phong).ThenInclude(p => p.LoaiPhong)
                .Where(c => c.NgayCheckIn >= start 
                         && c.NgayCheckIn <= end 
                         && c.Phong != null 
                         && c.Phong.LoaiPhong != null
                         && (c.DatPhong.TrangThaiDat == 2 || c.DatPhong.TrangThaiDat == 3))
                .AsEnumerable()
                .Where(c => c.Phong?.LoaiPhong?.TenLoaiPhong != null)
                .GroupBy(c => c.Phong.LoaiPhong.TenLoaiPhong)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            var prodValues = new ChartValues<int>();
            var prodLabels = new ObservableCollection<string>();
            foreach(var rp in roomProd)
            {
                prodValues.Add(rp.Count);
                prodLabels.Add(rp.Name);
            }

            if(prodValues.Count == 0)
            {
                prodValues.AddRange(new int[] { 30, 25, 45, 10 });
                prodLabels.Add("Standard");
                prodLabels.Add("Superior");
                prodLabels.Add("Deluxe");
                prodLabels.Add("Suite");
            }
            RoomProductivityLabels = prodLabels;

            RoomProductivitySeries = new SeriesCollection
            {
                new ColumnSeries { Title = "Số lượng", Values = prodValues, Fill = System.Windows.Media.Brushes.MediumSlateBlue }
            };

            // RoomStatusPieSeries: biểu đồ tròn "Trạng thái phòng" cho Tab 4 (phải)
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

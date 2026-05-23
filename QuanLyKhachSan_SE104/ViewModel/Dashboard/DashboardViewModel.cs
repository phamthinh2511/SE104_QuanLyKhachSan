using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.ViewModel.Dashboard
{
    public class ScheduleEventVM
    {
        public string CustomerName { get; set; } = "";
        public string EventType { get; set; } = "";
        public string TimeDisplay { get; set; } = "";
        public string Description { get; set; } = "";
        public int StartColumn { get; set; }
        public int ColumnSpan { get; set; } = 1;
        public string ColorBrush { get; set; } = "";
        public string BorderBrush { get; set; } = "";
        public string IndicatorColor { get; set; } = "";
        public string TextColor { get; set; } = "";
    }

    public class ScheduleRowVM
    {
        public string RoomName { get; set; } = "";
        public ObservableCollection<ScheduleEventVM> Events { get; set; } = new();
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadSchedule();
            }
        }

        private string _selectedFilter = "Tất cả";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public List<string> FiltersList { get; } = new() { "Tất cả", "Check-in", "Check-out", "Dịch vụ" };

        private List<RawEvent> _allRawEvents = new();

        public ObservableCollection<ScheduleRowVM> ScheduleRows { get; set; } = new();

        public DashboardViewModel()
        {
            SelectedDate = DateTime.Today; // triggers LoadSchedule()
        }

        public void LoadSchedule()
        {
            try
            {
                using var ctx = new QuanLyKhachSanContext();
                var targetDate = SelectedDate.Date;
                var rawEvents = new List<RawEvent>();

                // 1. Load Check-ins
                var checkIns = ctx.ChiTietDatPhongs
                    .Include(ct => ct.Phong)
                    .Include(ct => ct.DatPhong)
                        .ThenInclude(dp => dp.KhachHang)
                    .Where(ct => ct.NgayCheckIn.Date == targetDate)
                    .ToList();

                foreach (var ct in checkIns)
                {
                    rawEvents.Add(new RawEvent
                    {
                        RoomName = ct.Phong?.TenPhong ?? "Room",
                        CustomerName = ct.DatPhong?.KhachHang?.HoTen ?? "Khách hàng",
                        EventType = "Check-in",
                        Time = ct.NgayCheckIn,
                        TimeDisplay = ct.NgayCheckIn.ToString("HH:mm"),
                        Description = "Check-in"
                    });
                }

                // 2. Load Check-outs
                var checkOuts = ctx.ChiTietDatPhongs
                    .Include(ct => ct.Phong)
                    .Include(ct => ct.DatPhong)
                        .ThenInclude(dp => dp.KhachHang)
                    .Where(ct => ct.NgayCheckOut.Date == targetDate)
                    .ToList();

                foreach (var ct in checkOuts)
                {
                    rawEvents.Add(new RawEvent
                    {
                        RoomName = ct.Phong?.TenPhong ?? "Room",
                        CustomerName = ct.DatPhong?.KhachHang?.HoTen ?? "Khách hàng",
                        EventType = "Check-out",
                        Time = ct.NgayCheckOut,
                        TimeDisplay = ct.NgayCheckOut.ToString("HH:mm"),
                        Description = "Check-out"
                    });
                }

                // 3. Load Service Usages
                var services = ctx.ChiTietDichVus
                    .Include(ct => ct.DichVu)
                    .Include(ct => ct.ChiTietDatPhong)
                        .ThenInclude(ctdp => ctdp.Phong)
                    .Include(ct => ct.ChiTietDatPhong)
                        .ThenInclude(ctdp => ctdp.DatPhong)
                            .ThenInclude(dp => dp.KhachHang)
                    .Where(ct => ct.ThoiGianSuDung.Date == targetDate)
                    .ToList();

                foreach (var ct in services)
                {
                    rawEvents.Add(new RawEvent
                    {
                        RoomName = ct.ChiTietDatPhong?.Phong?.TenPhong ?? "Room",
                        CustomerName = ct.ChiTietDatPhong?.DatPhong?.KhachHang?.HoTen ?? "Khách lẻ",
                        EventType = "Dịch vụ",
                        Time = ct.ThoiGianSuDung,
                        TimeDisplay = ct.ThoiGianSuDung.ToString("HH:mm"),
                        Description = $"{ct.DichVu?.TenDichVu ?? "Dịch vụ"} (SL: {ct.SoLuong})"
                    });
                }

                _allRawEvents = rawEvents;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                // Fallback / Log error, avoid crashing
                Console.WriteLine("Error loading schedule: " + ex.Message);
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allRawEvents.AsEnumerable();

            if (SelectedFilter != "Tất cả")
            {
                filtered = filtered.Where(e => e.EventType == SelectedFilter);
            }

            var grouped = filtered
                .GroupBy(e => e.RoomName)
                .OrderBy(g => g.Key)
                .Select(g => new ScheduleRowVM
                {
                    RoomName = g.Key,
                    Events = new ObservableCollection<ScheduleEventVM>(
                        g.Select(e => {
                            int startCol = 0;
                            int hour = e.Time.Hour;
                            if (hour < 8) startCol = 0;
                            else if (hour > 22) startCol = 14;
                            else startCol = hour - 8;

                            string color = "#F0FDF4";
                            string border = "#DCFCE7";
                            string indicator = "#22C55E";
                            string text = "#15803D";

                            if (e.EventType == "Check-out")
                            {
                                color = "#FFF7ED";
                                border = "#FFEDD5";
                                indicator = "#F97316";
                                text = "#C2410C";
                            }
                            else if (e.EventType == "Dịch vụ")
                            {
                                color = "#EFF6FF";
                                border = "#DBEAFE";
                                indicator = "#3B82F6";
                                text = "#1D4ED8";
                            }

                            return new ScheduleEventVM
                            {
                                CustomerName = e.CustomerName,
                                EventType = e.EventType,
                                TimeDisplay = e.TimeDisplay,
                                Description = e.Description,
                                StartColumn = startCol,
                                ColumnSpan = 1,
                                ColorBrush = color,
                                BorderBrush = border,
                                IndicatorColor = indicator,
                                TextColor = text
                            };
                        }).ToList()
                    )
                }).ToList();

            ScheduleRows.Clear();
            foreach (var r in grouped)
            {
                ScheduleRows.Add(r);
            }
        }

        private class RawEvent
        {
            public string RoomName { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string EventType { get; set; } = "";
            public DateTime Time { get; set; }
            public string TimeDisplay { get; set; } = "";
            public string Description { get; set; } = "";
        }
    }
}
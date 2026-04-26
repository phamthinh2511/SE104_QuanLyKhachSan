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
using QuanLyKhachSan_SE104.View.KhachHang;

namespace QuanLyKhachSan_SE104.ViewModel.KhachHangVM
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<KhachHang> _listKhachHang;
        public ObservableCollection<KhachHang> ListKhachHang
        {
            get => _listKhachHang;
            set { _listKhachHang = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged();
                Search();
            }
        }

        private KhachHang _selectedKhachHang;
        public KhachHang SelectedKhachHang
        {
            get => _selectedKhachHang;
            set { _selectedKhachHang = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        public KhachHangViewModel()
        {
            ListKhachHang = new ObservableCollection<KhachHang>();
            LoadData();

            AddCommand = new RelayCommand<object>((p) => true, (p) => AddKhachHang());
            EditCommand = new RelayCommand<KhachHang>((p) => p != null, (p) => EditKhachHang(p));
            DeleteCommand = new RelayCommand<KhachHang>((p) => p != null, (p) => DeleteKhachHang(p));
            RefreshCommand = new RelayCommand<object>((p) => true, (p) => LoadData());
        }

        public void LoadData()
        {
            try
            {
                using (var db = new QuanLyKhachSanContext())
                {
                    var list = db.KhachHangs.ToList();
                    ListKhachHang = new ObservableCollection<KhachHang>(list);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void Search()
        {
            try
            {
                using (var db = new QuanLyKhachSanContext())
                {
                    var query = db.KhachHangs.AsQueryable();
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(k => k.HoTen.Contains(SearchText) || 
                                               k.SDT.Contains(SearchText) || 
                                               k.CCCD_Passport.Contains(SearchText));
                    }
                    ListKhachHang = new ObservableCollection<KhachHang>(query.ToList());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void AddKhachHang()
        {
            KhachHangDialog dialog = new KhachHangDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditKhachHang(KhachHang kh)
        {
            if (kh == null) return;
            KhachHangDialog dialog = new KhachHangDialog(kh);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteKhachHang(KhachHang kh)
        {
            if (kh == null) return;
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa khách hàng {kh.HoTen}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new QuanLyKhachSanContext())
                    {
                        // Kiểm tra xem khách hàng có đang trong booking nào không
                        bool hasBooking = db.DatPhongs.Any(d => d.MaKhachHang == kh.MaKhachHang);
                        if (hasBooking)
                        {
                            MessageBox.Show("Không thể xóa khách hàng này vì đã có lịch sử đặt phòng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        db.KhachHangs.Remove(kh);
                        db.SaveChanges();
                        LoadData();
                        MessageBox.Show("Xóa khách hàng thành công!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                }
            }
        }
    }
}

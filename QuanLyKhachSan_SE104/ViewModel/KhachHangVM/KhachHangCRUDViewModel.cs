using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;

namespace QuanLyKhachSan_SE104.ViewModel.KhachHangVM
{
        public class KhachHangCRUDViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            public Action CloseAction { get; set; }
            public Action<KhachHang> OnSaved { get; set; }

            private readonly KhachHang _khachHangGoc;

            // Các thuộc tính Binding
            private string _hoTen;
            public string HoTen { get => _hoTen; set { _hoTen = value; OnPropertyChanged(); } }

            private int _gioiTinh;
            public int GioiTinh { get => _gioiTinh; set { _gioiTinh = value; OnPropertyChanged(); } }

            private string _quocTich;
            public string QuocTich { get => _quocTich; set { _quocTich = value; OnPropertyChanged(); } }

            private string _cccd_Passport;
            public string CCCD_Passport { get => _cccd_Passport; set { _cccd_Passport = value; OnPropertyChanged(); } }

            private string _sdt;
            public string SDT { get => _sdt; set { _sdt = value; OnPropertyChanged(); } }

            private string _diaChi;
            public string DiaChi { get => _diaChi; set { _diaChi = value; OnPropertyChanged(); } }

            // Commands
            public ICommand LuuCommand { get; }
            public ICommand ThoatCommand { get; }

        public KhachHangCRUDViewModel(KhachHang item)
        {
            _khachHangGoc = item;

            // Load dữ liệu cũ lên UI
            HoTen = item.HoTen;
            // Ép kiểu an toàn từ String sang Int cho ComboBox
            GioiTinh = int.TryParse(item.GioiTinh, out int gt) ? gt : 0;
            QuocTich = item.QuocTich;
            CCCD_Passport = item.CCCD_Passport;
            SDT = item.SDT;
            DiaChi = item.DiaChi;

            LuuCommand = new RelayCommand(ExecuteLuu);
            ThoatCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        private void ExecuteLuu()
        {
            if (string.IsNullOrWhiteSpace(HoTen))
            { MessageBox.Show("Vui lòng nhập họ tên khách hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (string.IsNullOrWhiteSpace(CCCD_Passport))
            { MessageBox.Show("Vui lòng nhập CCCD/Passport.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var khDAL = new DAL.QuanLyKhachHangDAL();

            // Gán dữ liệu mới vào Object
            _khachHangGoc.HoTen = HoTen;
            _khachHangGoc.GioiTinh = GioiTinh.ToString(); // Đưa về lại kiểu string để đồng bộ UI hiện tại
            _khachHangGoc.QuocTich = QuocTich;
            _khachHangGoc.CCCD_Passport = CCCD_Passport;
            _khachHangGoc.SDT = SDT;
            _khachHangGoc.DiaChi = DiaChi;

            if (khDAL.Sua(_khachHangGoc))
            {
                OnSaved?.Invoke(_khachHangGoc);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra khi cập nhật thông tin.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}

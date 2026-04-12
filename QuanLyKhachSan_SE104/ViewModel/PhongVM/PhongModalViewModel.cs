using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.ViewModel.PhongVM 
{
    public class PhongModalViewModel : INotifyPropertyChanged
    {
        private int _maPhong;

        public int MaPhong
        {
            get { return _maPhong; }
            set { _maPhong = value; OnPropertyChanged(); }
        }
        private string _tenPhong;

        public string TenPhong
        {
            get { return _tenPhong; }
            set { _tenPhong = value; OnPropertyChanged(); }
        }
        private int _maLoaiPhong;

        public int MaLoaiPhong
        {
            get { return _maLoaiPhong; }
            set { _maLoaiPhong = value; OnPropertyChanged(); }
        }

        private int _soTang;

        public int SoTang
        {
            get { return _soTang; }
            set { _soTang = value; OnPropertyChanged(); }
        }

        private int _trangThai;

        public string TrangThai
        {
            get 
            {
                if (_trangThai == 0) return "Trống";
                if (_trangThai == 1) return "Đang ở";
                if (_trangThai == 2) return "Quá hạn";
                if (_trangThai == 3) return "Đa đặt";
                return "Đang bảo trì";
            }
            set 
            {
                if (value == "Đang ở") _trangThai = 1;
                if (value == "Trống") _trangThai = 0;
                if (value == "Quá hạn") _trangThai = 2;
                if (value == "Đa đặt") _trangThai = 3;
                if (value == "Bảo trì") _trangThai = 4;
                OnPropertyChanged();
            }
        }

        private int _trangThaiDonDep;

        public string TrangThaiDonDep
        {
            get
            {
                if(_trangThaiDonDep == 0) return "Cần dọn";
                if (_trangThai == 1) return "Đang dọn";
                return "Đa dọn";
            }
            set 
            {
                if (value == "Đang dọn") _trangThaiDonDep = 1;
                if (value == "Cần dọn") _trangThaiDonDep = 0;
                if (value == "Đa dọn") _trangThaiDonDep = 2;
                OnPropertyChanged(); 
            }
        }
        // Init

        public PhongModalViewModel()
        {

        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

}

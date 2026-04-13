using QuanLyKhachSan_SE104.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        private LoaiPhong _loaiPhong;
        public LoaiPhong Loaiphong
        {
            get { return _loaiPhong; }
            set { _loaiPhong = value; OnPropertyChanged(); }
        }

        private int _soTang;
        public int SoTang
        {
            get { return _soTang; }
            set { _soTang = value; OnPropertyChanged(); }
        }

        private List<LoaiPhong> _danhSachLoaiPhong;
        public List<LoaiPhong> DanhSachLoaiPhong
        {
            get { return _danhSachLoaiPhong; }
            set { _danhSachLoaiPhong = value; OnPropertyChanged(); }
        }

        private int _trangThai;
        // Getter/Setter raw value để code-behind dễ lấy data lưu DB
        public int TrangThaiValue
        {
            get { return _trangThai; }
            set { _trangThai = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrangThai)); }
        }

        public string TrangThai
        {
            get
            {
                if (_trangThai == 0) return "Trống";
                if (_trangThai == 1) return "Đã đặt";
                if (_trangThai == 2) return "Đang ở";
                return "Bảo trì";
            }
            set
            {
                if (value == "Trống") _trangThai = 0;
                else if (value == "Đã đặt") _trangThai = 1;
                else if (value == "Đang ở") _trangThai = 2;
                else if (value == "Bảo trì") _trangThai = 3;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrangThaiValue));
            }
        }

        private int _trangThaiDonDep;
        public int TrangThaiDonDepValue
        {
            get { return _trangThaiDonDep; }
            set { _trangThaiDonDep = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrangThaiDonDep)); }
        }

        public string TrangThaiDonDep
        {
            get
            {
                if (_trangThaiDonDep == 0) return "Đã dọn";
                if (_trangThaiDonDep == 1) return "Đang dọn";
                return "Cần dọn";
            }
            set
            {
                if (value == "Đã dọn") _trangThaiDonDep = 0;
                else if (value == "Đang dọn") _trangThaiDonDep = 1;
                else if (value == "Cần dọn") _trangThaiDonDep = 2;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrangThaiDonDepValue));
            }
        }
        public PhongModalViewModel(Phong p, List<LoaiPhong> dsLoaiPhong)
        {
            DanhSachLoaiPhong = dsLoaiPhong;

            if (p != null && p.MaPhong != 0)
            {
                MaPhong = p.MaPhong;
                TenPhong = p.TenPhong;
                SoTang = p.SoTang;
                TrangThaiValue = p.TrangThai;
                TrangThaiDonDepValue = p.TrangThaiDonDep;

                if (DanhSachLoaiPhong != null)
                {
                    Loaiphong = DanhSachLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == p.MaLoaiPhong);
                }
            }
        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
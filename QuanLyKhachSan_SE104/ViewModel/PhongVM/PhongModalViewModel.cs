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
        public LoaiPhong LoaiPhong
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
                switch (_trangThai)
                {
                    case 0: return "Trống";
                    case 1: return "Đã đặt";
                    case 2: return "Đang ở";
                    case 3: return "Quá hạn";
                    case 4: return "Cần dọn";
                    case 5: return "Bảo trì";
                    default: return "Bảo trì";
                }
            }
            set
            {
                switch (value)
                {
                    case "Trống": _trangThai = 0; break;
                    case "Đã đặt": _trangThai = 1; break;
                    case "Đang ở": _trangThai = 2; break;
                    case "Quá hạn": _trangThai = 3; break;
                    case "Cần dọn": _trangThai = 4; break;
                    case "Bảo trì": _trangThai = 5; break;
                }
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

            if (p != null)
            {
                MaPhong = p.MaPhong;
                TenPhong = p.TenPhong;
                SoTang = p.SoTang;
                TrangThaiValue = p.TrangThai;
                TrangThaiDonDepValue = p.TrangThaiDonDep;

                if (DanhSachLoaiPhong != null)
                {
                    LoaiPhong = DanhSachLoaiPhong.FirstOrDefault(x => x.MaLoaiPhong == p.MaLoaiPhong);
                }
            }
        }

        // ── INotifyPropertyChanged ────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
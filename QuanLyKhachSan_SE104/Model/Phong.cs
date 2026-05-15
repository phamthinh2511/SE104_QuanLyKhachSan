using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace QuanLyKhachSan_SE104.Model
{
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }

        public string TenPhong { get; set; }

        public int MaLoaiPhong { get; set; }

        public int SoTang { get; set; }

        // Thêm [Column] nếu bạn chưa chạy Migration để đổi tên cột trong Database
        [Column("TrangThaiThue")]
        public int TrangThai { get; set; }

        public int TrangThaiDonDep { get; set; }

        public LoaiPhong LoaiPhong { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }

        private bool _isCheckInToday;
        [NotMapped] // Đánh dấu để Entity Framework không tạo cột này trong DB
        public bool IsCheckInToday
        {
            get => _isCheckInToday;
            set { _isCheckInToday = value; OnPropertyChanged(); }
        }

        private bool _isCheckOutToday;
        [NotMapped]
        public bool IsCheckOutToday
        {
            get => _isCheckOutToday;
            set { _isCheckOutToday = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
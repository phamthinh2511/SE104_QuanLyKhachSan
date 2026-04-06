using System.Collections.ObjectModel;
using System.Linq;
using QuanLyKhachSan_SE104.Model;

namespace QuanLyKhachSan_SE104.ViewModel
{
    public class NhanPhongViewModel
    {
        // Danh sách các phòng sắp check-in
        public ObservableCollection<ChiTietDatPhong> UpcomingCheckins { get; set; }

        public NhanPhongViewModel()
        {
            // Trong thực tế, bạn sẽ gọi Database ở đây: 
            // db.ChiTietDatPhongs.Where(x => x.DatPhong.TrangThaiDat == 1).ToList();

            UpcomingCheckins = new ObservableCollection<ChiTietDatPhong>()
            {
                new ChiTietDatPhong
                {
                    MaChiTietDatPhong = 1,
                    MaPhong = 101,
                    Phong = new Phong { TenPhong = "101", LoaiPhong = new LoaiPhong { TenLoaiPhong = "Standard" } },
                    NgayCheckIn = DateTime.Now,
                    GiaDat = 500000,
                    DatPhong = new QuanLyKhachSan_SE104.Model.DatPhong
                    {
                        KhachHang = new KhachHang { HoTen = "Nguyễn Văn A" },
                        TienCoc = 200000,
                        TrangThaiDat = 1
                    }
                },
                new ChiTietDatPhong
                {
                    MaChiTietDatPhong = 2,
                    MaPhong = 202,
                    Phong = new Phong { TenPhong = "202", LoaiPhong = new LoaiPhong { TenLoaiPhong = "VIP Single" } },
                    NgayCheckIn = DateTime.Now.AddHours(2),
                    GiaDat = 1200000,
                    DatPhong = new QuanLyKhachSan_SE104.Model.DatPhong
                    {
                        KhachHang = new KhachHang { HoTen = "Trần Thị B" },
                        TienCoc = 500000,
                        TrangThaiDat = 1
                    }
                }
            };
        }
    }
}
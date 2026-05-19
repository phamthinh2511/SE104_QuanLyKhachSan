namespace QuanLyKhachSan_SE104.DTOs
{
    public class RoomSearchDTO
    {
        public DateTime NgayCheckIn { get; set; }
        public DateTime NgayCheckOut { get; set; }
        public int? SoTang { get; set; }
        public int? MaLoaiPhong { get; set; }
    }
}

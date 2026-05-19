namespace QuanLyKhachSan_SE104.DTOs
{
    public class BookingResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? MaDatPhong { get; set; }
        public bool IsConflict { get; set; }

        public static BookingResult Success(int id)
        {
            return new BookingResult
            {
                IsSuccess = true,
                Message = "Thao tac dat phong thanh cong.",
                MaDatPhong = id,
                IsConflict = false
            };
        }

        public static BookingResult Conflict(int maPhong)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Message = $"Phong #{maPhong} da co nguoi dat trong khoang thoi gian nay.",
                IsConflict = true
            };
        }

        public static BookingResult ValidationError(string msg)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Message = msg,
                IsConflict = false
            };
        }

        public static BookingResult Error(string msg)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Message = msg,
                IsConflict = false
            };
        }
    }
}

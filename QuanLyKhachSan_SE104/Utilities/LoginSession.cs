namespace QuanLyKhachSan_SE104.Utilities
{
    public static class LoginSession
    {
        // Lưu ID và thông tin của nhân viên đang đăng nhập sau khi verify tài khoản thành công
        public static int CurrentNhanVienId { get; set; }
        public static string CurrentNhanVienName { get; set; }
        public static bool IsAdmin { get; set; }
    }
}
namespace QuanLyKhachSan_SE104.Utilities
{
    public static class HotelEventBus
    {
        public static event Action RoomStatusChanged;

        public static void PublishRoomStatusChanged()
            => RoomStatusChanged?.Invoke();
    }
}
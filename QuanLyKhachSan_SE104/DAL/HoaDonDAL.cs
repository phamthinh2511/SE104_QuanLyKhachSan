using MySqlConnector;
using QuanLyKhachSan_SE104.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DAL
{
    public class HoaDonDAL
    {
        private string connectionString = "Server=cnpm.mysql.database.azure.com;Port=3306;Database=hotelmanagement;Uid=adminuser;Pwd=cnpm123#;SslMode=Required;";

        public HoaDonChiTietDTO LayChiTietHoaDonTheoPhong(int maChiTietDatPhong)
        {
            HoaDonChiTietDTO hoaDon = null;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // 1. Lấy thông tin chung của phòng & hóa đơn
                string queryThongTin = @"
                    SELECT 
                        hd.MaHoaDon, dp.MaDatPhong, kh.HoTen AS TenKhachHang, kh.SDT,
                        p.TenPhong, ct.NgayCheckIn, ct.NgayCheckOut, ct.GiaDat, 
                        nv.HoTen AS TenNhanVien, dp.TienCoc, hd.TongTienPhong, hd.TongTienDichVu, hd.TongThanhToan
                    FROM chitietdatphongs ct
                    JOIN datphongs dp ON ct.MaDatPhong = dp.MaDatPhong
                    JOIN khachhangs kh ON dp.MaKhachHang = kh.MaKhachHang
                    JOIN phongs p ON ct.MaPhong = p.MaPhong
                    LEFT JOIN nhanviens nv ON dp.MaNhanVien = nv.MaNhanVien
                    LEFT JOIN hoadons hd ON dp.MaDatPhong = hd.MaDatPhong
                    WHERE ct.MaChiTietDatPhong = @MaCTDP";

                using (MySqlCommand cmd = new MySqlCommand(queryThongTin, conn))
                {
                    cmd.Parameters.AddWithValue("@MaCTDP", maChiTietDatPhong);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DateTime checkIn = reader.GetDateTime("NgayCheckIn");
                            DateTime checkOut = reader.GetDateTime("NgayCheckOut");
                            int soDem = (checkOut.Date - checkIn.Date).Days;
                            if (soDem <= 0) soDem = 1; // Ở trong ngày tính 1 đêm

                            int maHD = reader.IsDBNull(reader.GetOrdinal("MaHoaDon")) ? 0 : reader.GetInt32("MaHoaDon");

                            hoaDon = new HoaDonChiTietDTO
                            {
                                MaHoaDonFormatted = maHD > 0 ? $"#HD-{maHD:D6}" : "#CHUA_THANH_TOAN",
                                MaDatPhong = reader.GetInt32("MaDatPhong"),
                                TenKhachHang = reader.GetString("TenKhachHang"),
                                SDT = reader.IsDBNull(reader.GetOrdinal("SDT")) ? "" : reader.GetString("SDT"),
                                TenPhong = reader.GetString("TenPhong"),
                                NgayCheckIn = checkIn,
                                NgayCheckOut = checkOut,
                                SoDem = soDem,
                                TenNhanVien = reader.IsDBNull(reader.GetOrdinal("TenNhanVien")) ? "" : reader.GetString("TenNhanVien"),

                                TienCoc = reader.IsDBNull(reader.GetOrdinal("TienCoc")) ? 0 : reader.GetDecimal("TienCoc"),
                                // Ước tính tiền phòng nếu chưa có hóa đơn (Số đêm * Giá đặt)
                                TienPhong = reader.IsDBNull(reader.GetOrdinal("TongTienPhong")) ? (soDem * reader.GetDecimal("GiaDat")) : reader.GetDecimal("TongTienPhong"),
                                TienDichVu = reader.IsDBNull(reader.GetOrdinal("TongTienDichVu")) ? 0 : reader.GetDecimal("TongTienDichVu"),
                                TongThanhToan = reader.IsDBNull(reader.GetOrdinal("TongThanhToan")) ? 0 : reader.GetDecimal("TongThanhToan")
                            };
                        }
                    }
                }

                // 2. Lấy danh sách dịch vụ mà phòng này đã dùng
                if (hoaDon != null)
                {
                    string queryDichVu = @"
                        SELECT dv.TenDichVu, ctdv.SoLuong, ctdv.DonGia
                        FROM chitietdichvus ctdv
                        JOIN dichvus dv ON ctdv.MaDichVu = dv.MaDichVu
                        WHERE ctdv.MaChiTietDatPhong = @MaCTDP";

                    using (MySqlCommand cmdDV = new MySqlCommand(queryDichVu, conn))
                    {
                        cmdDV.Parameters.AddWithValue("@MaCTDP", maChiTietDatPhong);
                        using (MySqlDataReader readerDV = cmdDV.ExecuteReader())
                        {
                            decimal tongTienDVThucTe = 0;
                            while (readerDV.Read())
                            {
                                int sl = readerDV.GetInt32("SoLuong");
                                decimal dg = readerDV.GetDecimal("DonGia");
                                decimal thanhTien = sl * dg;
                                tongTienDVThucTe += thanhTien;

                                hoaDon.DanhSachDichVu.Add(new DichVuDaDungDTO
                                {
                                    TenDichVu = readerDV.GetString("TenDichVu"),
                                    SoLuong = sl,
                                    ThanhTien = thanhTien
                                });
                            }

                            // Cập nhật lại tổng tiền nếu hóa đơn chưa chốt
                            if (hoaDon.MaHoaDonFormatted == "#CHUA_THANH_TOAN")
                            {
                                hoaDon.TienDichVu = tongTienDVThucTe;
                                hoaDon.TongThanhToan = hoaDon.TienPhong + hoaDon.TienDichVu - hoaDon.TienCoc;
                            }
                        }
                    }
                }
            }
            return hoaDon;
        }
    }
}

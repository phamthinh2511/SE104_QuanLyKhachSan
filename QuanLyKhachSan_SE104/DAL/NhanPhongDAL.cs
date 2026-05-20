using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DAL
{
    public class NhanPhongDAL
    {
        private string connectionString = "Server = cnpm.mysql.database.azure.com; Port = 3306; Database = hotelmanagement; Uid = adminuser; Pwd = cnpm123#; SslMode=Required;\r\n";

        // Lấy danh sách khách dự kiến nhận phòng hôm nay (Trạng thái đặt = 1: Đã xác nhận)
        public List<ThongTinNhanPhongDTO> LayDanhSachNhanPhongDuKien()
        {
            List<ThongTinNhanPhongDTO> list = new List<ThongTinNhanPhongDTO>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Lấy các phòng có NgayCheckIn là hôm nay và TrangThaiDat = 1
                string query = @"
                SELECT ct.MaChiTietDatPhong, dp.MaDatPhong, kh.MaKhachHang, p.MaPhong,
                       kh.HoTen, lp.TenLoaiPhong, p.TenPhong, ct.NgayCheckIn, dp.TienCoc, kh.CCCD_Passport, ct.SoNguoi
                FROM chitietdatphongs ct
                JOIN datphongs dp ON ct.MaDatPhong = dp.MaDatPhong
                JOIN khachhangs kh ON dp.MaKhachHang = kh.MaKhachHang
                JOIN phongs p ON ct.MaPhong = p.MaPhong
                JOIN loaiphongs lp ON p.MaLoaiPhong = lp.MaLoaiPhong
                WHERE DATE(ct.NgayCheckIn) = CURDATE() 
                    AND ct.TrangThaiSegment = 0
                    AND p.TrangThaiThue = 1";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ThongTinNhanPhongDTO
                        {
                            MaChiTietDatPhong = reader.GetInt32("MaChiTietDatPhong"),
                            MaDatPhong = reader.GetInt32("MaDatPhong"),
                            MaKhachHang = reader.GetInt32("MaKhachHang"),
                            MaPhong = reader.GetInt32("MaPhong"),
                            TenKhachHang = reader.GetString("HoTen"),
                            TenLoaiPhong = reader.GetString("TenLoaiPhong"),
                            TenPhong = reader.GetString("TenPhong"),
                            NgayCheckInDuKien = reader.GetDateTime("NgayCheckIn"),
                            TienCoc = reader.IsDBNull(reader.GetOrdinal("TienCoc")) ? 0 : reader.GetDecimal("TienCoc"),
                            CCCD_Passport = reader.IsDBNull(reader.GetOrdinal("CCCD_Passport")) ? "" : reader.GetString("CCCD_Passport"),
                            SoNguoi = reader.GetInt32("SoNguoi")
                        });
                    }
                }
            }
            return list;
        }

        // Nghiệp vụ xác nhận check-in
        public bool XacNhanNhanPhong(int maChiTietDatPhong, int maPhong, int maKhachHang, string cccdThucTe, int soNguoiThucTe, int maDatPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Cập nhật CCCD khách hàng (nếu có bổ sung)
                        if (!string.IsNullOrEmpty(cccdThucTe))
                        {
                            string updateKhachHang = "UPDATE khachhangs SET CCCD_Passport = @cccd WHERE MaKhachHang = @maKH";
                            using (MySqlCommand cmd = new MySqlCommand(updateKhachHang, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@cccd", cccdThucTe);
                                cmd.Parameters.AddWithValue("@maKH", maKhachHang);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 2. Cập nhật chi tiết đặt phòng (Số người thực tế & thời gian check-in thực tế)
                        string updateChiTiet = "UPDATE chitietdatphongs SET SoNguoi = @soNguoi, NgayCheckIn = NOW(), TrangThaiSegment = 1 WHERE MaChiTietDatPhong = @maCT";
                        using (MySqlCommand cmd = new MySqlCommand(updateChiTiet, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@soNguoi", soNguoiThucTe);
                            cmd.Parameters.AddWithValue("@maCT", maChiTietDatPhong);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Cập nhật trạng thái PHONG sang Đang ở (2)
                        string updatePhong = "UPDATE phongs SET TrangThaiThue = 2 WHERE MaPhong = @maPhong";
                        using (MySqlCommand cmd = new MySqlCommand(updatePhong, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@maPhong", maPhong);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. KIỂM TRA & CẬP NHẬT DATPHONG
                        // Đếm xem trong phiếu này còn phòng nào chưa check-in (TrangThaiThue khác 2) không
                        string checkRemainingRooms = @"
                            SELECT COUNT(*) 
                            FROM CHITIETDATPHONG ct
                            JOIN PHONG p ON ct.MaPhong = p.MaPhong
                            WHERE ct.MaDatPhong = @maDatPhong AND p.TrangThaiThue != 2";

                        int remainingRooms = 0;
                        using (MySqlCommand cmd = new MySqlCommand(checkRemainingRooms, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@maDatPhong", maDatPhong);
                            remainingRooms = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Nếu không còn phòng nào chờ check-in, update cả phiếu thành "Đã nhận phòng" (2)
                        if (remainingRooms == 0)
                        {
                            string updateDatPhong = "UPDATE DATPHONG SET TrangThaiDat = 2 WHERE MaDatPhong = @maDatPhong";
                            using (MySqlCommand cmd = new MySqlCommand(updateDatPhong, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@maDatPhong", maDatPhong);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}

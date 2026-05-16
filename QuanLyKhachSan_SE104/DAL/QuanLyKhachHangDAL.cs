using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DAL
{
    public class QuanLyKhachHangDAL
    {
        private string connectionString = "Server = cnpm.mysql.database.azure.com; Port = 3306; Database = hotelmanagement; Uid = adminuser; Pwd = cnpm123#; SslMode=Required;\r\n";

        public List<KhachHang> LayDanhSach(string key = "")
        {
            List<KhachHang> list = new List<KhachHang>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM khachhangs WHERE IsDeleted = 0";

                // Nếu có từ khóa, thêm điều kiện LIKE
                if (!string.IsNullOrWhiteSpace(key))
                {
                    query += " AND HoTen LIKE @TuKhoa";
                }

                query += " ORDER BY HoTen ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        // Thêm % để tìm kiếm chuỗi chứa từ khóa
                        cmd.Parameters.AddWithValue("@TuKhoa", $"%{key}%");
                    }

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new KhachHang
                            {
                                MaKhachHang = reader.GetInt32("MaKhachHang"),
                                HoTen = reader.GetString("HoTen"),
                                GioiTinh = reader.GetString("GioiTinh"),
                                QuocTich = reader.IsDBNull(reader.GetOrdinal("QuocTich")) ? "" : reader.GetString("QuocTich"),
                                CCCD_Passport = reader.IsDBNull(reader.GetOrdinal("CCCD_Passport")) ? "" : reader.GetString("CCCD_Passport"),
                                SDT = reader.IsDBNull(reader.GetOrdinal("SDT")) ? "" : reader.GetString("SDT"),
                                DiaChi = reader.IsDBNull(reader.GetOrdinal("DiaChi")) ? "" : reader.GetString("DiaChi"),
                                IsDeleted = false
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool Sua(KhachHang item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE khachhangs 
                         SET HoTen = @HoTen, GioiTinh = @GioiTinh, QuocTich = @QuocTich, 
                             CCCD_Passport = @CCCD, SDT = @SDT, DiaChi = @DiaChi 
                         WHERE MaKhachHang = @MaKhachHang";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@HoTen", item.HoTen);
                    cmd.Parameters.AddWithValue("@GioiTinh", item.GioiTinh);
                    cmd.Parameters.AddWithValue("@QuocTich", item.QuocTich);
                    cmd.Parameters.AddWithValue("@CCCD", item.CCCD_Passport);
                    cmd.Parameters.AddWithValue("@SDT", item.SDT);
                    cmd.Parameters.AddWithValue("@DiaChi", item.DiaChi);
                    cmd.Parameters.AddWithValue("@MaKhachHang", item.MaKhachHang);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

    }
}

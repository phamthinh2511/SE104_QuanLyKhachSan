using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DAL
{
    public class QuanLyPhongDAL
    {
        private String connectionString = "Server=cnpm.mysql.database.azure.com;Port=3306;Database=hotelmanagement;Uid=adminuser;Pwd=cnpm123#;SslMode=Required;";

        public List<Phong> LayDanhSachActive()
        {
            List<Phong> list = new List<Phong>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT p.MaPhong, p.TenPhong, p.MaLoaiPhong, p.SoTang, p.TrangThaiThue, p.TrangThaiDonDep,
                           lp.TenLoaiPhong 
                    FROM phongs p
                    JOIN loaiphongs lp ON p.MaLoaiPhong = lp.MaLoaiPhong
                    WHERE p.IsDeleted = 0
                    ORDER BY TenPhong ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapPhongFromReader(reader));
                    }
                }
            }
            return list;
        }

        public List<Phong> LayDanhSachTatCa()
        {
            List<Phong> list = new List<Phong>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                                SELECT p.*, lp.TenLoaiPhong 
                                FROM phongs p
                                JOIN loaiphongs lp ON p.MaLoaiPhong = lp.MaLoaiPhong
                                ORDER BY p.TenPhong ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapPhongFromReader(reader));
                    }
                }
            }
            return list;
        }

        private Phong MapPhongFromReader(MySqlDataReader reader)
        {
            return new Phong
            {
                MaPhong = reader.GetInt32("MaPhong"),
                TenPhong = reader.GetString("TenPhong"),
                MaLoaiPhong = reader.GetInt32("MaLoaiPhong"),
                SoTang = reader.GetInt32("SoTang"),
                TrangThai = reader.IsDBNull(reader.GetOrdinal("TrangThaiThue")) ? (byte)0 : reader.GetByte("TrangThaiThue"),
                TrangThaiDonDep = reader.IsDBNull(reader.GetOrdinal("TrangThaiDonDep")) ? (byte)0 : reader.GetByte("TrangThaiDonDep"),

                IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean("IsDeleted"),

                LoaiPhong = new LoaiPhong
                {
                    MaLoaiPhong = reader.GetInt32("MaLoaiPhong"),
                    TenLoaiPhong = reader.GetString("TenLoaiPhong")
                }
            };
        }

        public bool Them(Phong item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO phongs (TenPhong, MaLoaiPhong, SoTang, TrangThaiThue, TrangThaiDonDep, IsDeleted) 
                                 VALUES (@Ten, @MaLoai, @SoTang, 0, 0, 0);
                                 SELECT LAST_INSERT_ID();";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", item.TenPhong);
                    cmd.Parameters.AddWithValue("@MaLoai", item.MaLoaiPhong);
                    cmd.Parameters.AddWithValue("@SoTang", item.SoTang);

                    item.MaPhong = Convert.ToInt32(cmd.ExecuteScalar());
                    return item.MaPhong > 0;
                }
            }
        }

        public bool Sua(Phong item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE phongs 
                                 SET TenPhong = @Ten, MaLoaiPhong = @MaLoai, SoTang = @SoTang 
                                 WHERE MaPhong = @MaPhong";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", item.TenPhong);
                    cmd.Parameters.AddWithValue("@MaLoai", item.MaLoaiPhong);
                    cmd.Parameters.AddWithValue("@SoTang", item.SoTang);
                    cmd.Parameters.AddWithValue("@MaPhong", item.MaPhong);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Xoa(int maPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE phongs SET IsDeleted = 1 WHERE MaPhong = @MaPhong";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool HoanTacXoa(int maPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE phongs SET IsDeleted = 0 WHERE MaPhong = @MaPhong";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public string KiemTraDieuKienXoa(int maPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TrangThaiThue FROM phongs WHERE MaPhong = @MaPhong";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        int trangThai = Convert.ToInt32(result);
                        if (trangThai == 1) return "Phòng đã được khách đặt. Không thể xóa!";
                        if (trangThai == 2) return "Phòng đang có khách lưu trú. Không thể xóa!";
                    }
                }
            }
            return ""; // Chuỗi rỗng nghĩa là đủ điều kiện xóa hợp lệ
        }
    }
}

using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_SE104.DAL
{
    public class QuanLyLoaiPhongDAL
    {
        private String connectionString = "Server=cnpm.mysql.database.azure.com;Port=3306;Database=hotelmanagement;Uid=adminuser;Pwd=cnpm123#;SslMode=Required;";

        public List<LoaiPhong> LayDanhSachActive()
        {
            List<LoaiPhong> list = new List<LoaiPhong>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM loaiphongs WHERE IsDeleted = 0 ORDER BY TenLoaiPhong ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LoaiPhong
                        {
                            MaLoaiPhong = reader.GetInt32("MaLoaiPhong"),
                            TenLoaiPhong = reader.GetString("TenLoaiPhong"),
                            GiaMacDinh = reader.GetDecimal("GiaMacDinh"),
                            SoGiuong = reader.GetInt32("SoGiuong"),
                            SoNguoiToiDa = reader.GetInt32("SoNguoiToiDa"),
                            PhuPhiThemGio = reader.GetDecimal("PhuPhiThemGio"),
                            IsDeleted = false
                        });
                    }
                }
            }
            return list;
        }

        public List<LoaiPhong> LayDanhSachTatCa()
        {
            List<LoaiPhong> list = new List<LoaiPhong>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM loaiphongs ORDER BY TenLoaiPhong ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LoaiPhong
                        {
                            MaLoaiPhong = reader.GetInt32("MaLoaiPhong"),
                            TenLoaiPhong = reader.GetString("TenLoaiPhong"),
                            GiaMacDinh = reader.GetDecimal("GiaMacDinh"),
                            SoGiuong = reader.GetInt32("SoGiuong"),
                            SoNguoiToiDa = reader.GetInt32("SoNguoiToiDa"),
                            PhuPhiThemGio = reader.GetDecimal("PhuPhiThemGio"),
                            IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean("IsDeleted")
                        });
                    }
                }
            }
            return list;
        }

        public bool Them(LoaiPhong item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Dùng SELECT LAST_INSERT_ID() để lấy ngay ID vừa tạo gán lại cho object
                string query = @"INSERT INTO loaiphongs (TenLoaiPhong, GiaMacDinh, SoGiuong, SoNguoiToiDa, PhuPhiThemGio, IsDeleted) 
                                 VALUES (@Ten, @Gia, @Giuong, @Nguoi, @PhuPhi, 0);
                                 SELECT LAST_INSERT_ID();";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", item.TenLoaiPhong);
                    cmd.Parameters.AddWithValue("@Gia", item.GiaMacDinh);
                    cmd.Parameters.AddWithValue("@Giuong", item.SoGiuong);
                    cmd.Parameters.AddWithValue("@Nguoi", item.SoNguoiToiDa);
                    cmd.Parameters.AddWithValue("@PhuPhi", item.PhuPhiThemGio);

                    // ExecuteScalar trả về ID vừa sinh ra
                    item.MaLoaiPhong = Convert.ToInt32(cmd.ExecuteScalar());
                    return item.MaLoaiPhong > 0;
                }
            }
        }

        public bool Sua(LoaiPhong item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE loaiphongs 
                                 SET TenLoaiPhong = @Ten, GiaMacDinh = @Gia, SoGiuong = @Giuong, 
                                     SoNguoiToiDa = @Nguoi, PhuPhiThemGio = @PhuPhi 
                                 WHERE MaLoaiPhong = @MaLoaiPhong";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", item.TenLoaiPhong);
                    cmd.Parameters.AddWithValue("@Gia", item.GiaMacDinh);
                    cmd.Parameters.AddWithValue("@Giuong", item.SoGiuong);
                    cmd.Parameters.AddWithValue("@Nguoi", item.SoNguoiToiDa);
                    cmd.Parameters.AddWithValue("@PhuPhi", item.PhuPhiThemGio);
                    cmd.Parameters.AddWithValue("@MaLoaiPhong", item.MaLoaiPhong);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Xoa(int maLoaiPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE loaiphongs SET IsDeleted = 1 WHERE MaLoaiPhong = @MaLoaiPhong";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaLoaiPhong", maLoaiPhong);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool HoanTacXoa(int maLoaiPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE loaiphongs SET IsDeleted = 0 WHERE MaLoaiPhong = @MaLoaiPhong";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaLoaiPhong", maLoaiPhong);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public string KiemTraDieuKienXoa(int maLoaiPhong)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Kiểm tra xem có phòng nào thuộc loại này đang có khách/được đặt không
                string query1 = @"SELECT COUNT(*) FROM phongs 
                          WHERE MaLoaiPhong = @MaLoaiPhong 
                            AND IsDeleted = 0 
                            AND TrangThaiThue IN (1, 2)";
                using (MySqlCommand cmd1 = new MySqlCommand(query1, conn))
                {
                    cmd1.Parameters.AddWithValue("@MaLoaiPhong", maLoaiPhong);
                    int countBusyRooms = Convert.ToInt32(cmd1.ExecuteScalar());
                    if (countBusyRooms > 0)
                        return "Đang có khách ở hoặc đặt phòng thuộc loại này. Không thể xóa!";
                }

                // Kiểm tra xem có bất kỳ phòng nào (dù trống) đang dùng loại phòng này không
                string query2 = @"SELECT COUNT(*) FROM phongs 
                          WHERE MaLoaiPhong = @MaLoaiPhong 
                            AND IsDeleted = 0";
                using (MySqlCommand cmd2 = new MySqlCommand(query2, conn))
                {
                    cmd2.Parameters.AddWithValue("@MaLoaiPhong", maLoaiPhong);
                    int countActiveRooms = Convert.ToInt32(cmd2.ExecuteScalar());
                    if (countActiveRooms > 0)
                        return "Vẫn còn phòng hoạt động thuộc loại phòng này. Vui lòng xóa các phòng đó trước!";
                }
            }
            return ""; // Đủ điều kiện xóa
        }
    }
}


using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace QuanLyKhachSan_SE104.DAL
{
    public class QuanLyDichVuDAL
    {
        private String connectionString = "Server=cnpm.mysql.database.azure.com;Port=3306;Database=hotelmanagement;Uid=adminuser;Pwd=cnpm123#;SslMode=Required;";

        public List<DichVu> LayDanhSachActive()
        {
            List<DichVu> list = new List<DichVu>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM dichvus WHERE IsDeleted = 0 ORDER BY TenDichVu ASC";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapDichVu(reader));
                    }
                }
                return list;
            }
        }

        public List<DichVu> LayDanhSachTatCa()
        {
            List<DichVu> list = new List<DichVu>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM dichvus ORDER BY TenDichVu ASC";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(MapDichVu(reader));
                }
            }
            return list;
        }

        private DichVu MapDichVu(MySqlDataReader reader)
        {
            return new DichVu
            {
                MaDichVu = reader.GetInt32("MaDichVu"),
                LoaiDichVu = reader.GetByte("LoaiDichVu"),
                TenDichVu = reader.GetString("TenDichVu"),
                DonGia = reader.GetDecimal("DonGia"),
                MoTa = reader.IsDBNull(reader.GetOrdinal("MoTa")) ? "" : reader.GetString("MoTa"),
                IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean("IsDeleted")
            };
        }

        public bool Them(DichVu item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO dichvus (LoaiDichVu, TenDichVu, DonGia, MoTa, IsDeleted) 
                                 VALUES (@Loai, @Ten, @Gia, @MoTa, 0);
                                 SELECT LAST_INSERT_ID();";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Loai", item.LoaiDichVu);
                    cmd.Parameters.AddWithValue("@Ten", item.TenDichVu);
                    cmd.Parameters.AddWithValue("@Gia", item.DonGia);
                    cmd.Parameters.AddWithValue("@MoTa", item.MoTa);
                    item.MaDichVu = Convert.ToInt32(cmd.ExecuteScalar());
                    return item.MaDichVu > 0;
                }
            }
        }

        public bool Sua(DichVu item)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE dichvus
                                 SET LoaiDichVu = @Loai, TenDichVu = @Ten, DonGia = @Gia, MoTa = @MoTa 
                                 WHERE MaDichVu = @MaDichVu";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Loai", item.LoaiDichVu);
                    cmd.Parameters.AddWithValue("@Ten", item.TenDichVu);
                    cmd.Parameters.AddWithValue("@Gia", item.DonGia);
                    cmd.Parameters.AddWithValue("@MoTa", item.MoTa);
                    cmd.Parameters.AddWithValue("@MaDichVu", item.MaDichVu);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Xoa(int maDichVu)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE dichvus SET IsDeleted = 1 WHERE MaDichVu = @MaDichVu";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaDichVu", maDichVu);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool HoanTacXoa(int maDichVu)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE dichvus SET IsDeleted = 0 WHERE MaDichVu = @MaDichVu";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaDichVu", maDichVu);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public string KiemTraDieuKienXoa(int maDichVu)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Check xem dịch vụ này có đang nằm trong phiếu đặt phòng nào chưa thanh toán (TrangThaiDat < 3) không
                string query = @"SELECT COUNT(*) FROM chitietdichvus ct
                                 JOIN chitietdatphongs ctdp ON ct.MaChiTietDatPhong = ctdp.MaChiTietDatPhong
                                 JOIN datphongs dp ON ctdp.MaDatPhong = dp.MaDatPhong
                                 WHERE ct.MaDichVu = @MaDichVu AND dp.TrangThaiDat IN (1, 2)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaDichVu", maDichVu);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count > 0) return "Dịch vụ đang được khách hàng sử dụng, không thể xóa!";
                }
            }
            return "";
        }
    }
}

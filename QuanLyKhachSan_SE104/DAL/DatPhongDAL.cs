using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuanLyKhachSan_SE104.DTO;

namespace QuanLyKhachSan_SE104.DAL
{
    public class DatPhongDAL
    {
        // NOTE: Move this connection string to App.config or appsettings.json — never hardcode credentials in source code.
        private readonly string _connectionString =
            "Server=cnpm.mysql.database.azure.com;Port=3306;Database=hotelmanagement;Uid=adminuser;Pwd=cnpm123#;SslMode=Required;";

        // Returns list for ChiTietDatPhongPage.xaml ListDatPhong binding.
        // Columns mapped: MaDatPhong, NgayDat, TenKhachHang, SDT, TenNhanVien, TrangThaiDat, TienCoc
        public List<DatPhongDTO> LayDanhSachDatPhong()
        {
            var list = new List<DatPhongDTO>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string query = @"
                SELECT dp.MaDatPhong, dp.NgayDat, dp.TrangThaiDat, dp.TienCoc,
                       kh.HoTen AS TenKH, kh.SDT,
                       nv.HoTen AS TenNV
                FROM datphongs dp
                JOIN khachhangs kh ON dp.MaKhachHang = kh.MaKhachHang
                JOIN nhanviens nv  ON dp.MaNhanVien  = nv.MaNhanVien
                ORDER BY dp.NgayDat DESC";

            using var cmd = new MySqlCommand(query, conn);
            using var rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                list.Add(new DatPhongDTO
                {
                    MaDatPhong = rdr.GetInt32("MaDatPhong"),
                    NgayDat = rdr.GetDateTime("NgayDat"),
                    TrangThaiDat = rdr.GetInt32("TrangThaiDat"),
                    TienCoc = rdr.GetDecimal("TienCoc"),
                    TenKhachHang = rdr.GetString("TenKH"),
                    SDT = rdr.GetString("SDT"),
                    TenNhanVien = rdr.GetString("TenNV")
                });
            }

            return list;
        }

        // Returns detail rows for ChiTietDatPhongWindow.xaml DanhSachChiTiet binding.
        // Columns mapped: TenPhong, NgayCheckIn, NgayCheckOut, SoNguoi
        public List<ChiTietPhongDTO> LayChiTietCacPhong(int maDatPhong)
        {
            var list = new List<ChiTietPhongDTO>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string query = @"
                SELECT p.TenPhong,
                       ct.NgayCheckIn, ct.NgayCheckOut,
                       ct.SoNguoi
                FROM chitietdatphongs ct
                JOIN phongs p ON ct.MaPhong = p.MaPhong
                WHERE ct.MaDatPhong = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", maDatPhong);

            using var rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                list.Add(new ChiTietPhongDTO
                {
                    TenPhong = rdr.GetString("TenPhong"),
                    NgayCheckIn = rdr.GetDateTime("NgayCheckIn"),
                    NgayCheckOut = rdr.GetDateTime("NgayCheckOut"),
                    SoNguoi = rdr.GetInt32("SoNguoi")
                });
            }

            return list;
        }

        // Fix: original query targeted "datphongs" (with 's') — corrected to "datphong"
        public bool XoaDatPhong(int maDatPhong)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Also delete child rows first to avoid FK constraint violation
            using var cmdDetail = new MySqlCommand(
                "DELETE FROM chitietdatphongs WHERE MaDatPhong = @id", conn);
            cmdDetail.Parameters.AddWithValue("@id", maDatPhong);
            cmdDetail.ExecuteNonQuery();

            using var cmd = new MySqlCommand(
                "DELETE FROM datphongs WHERE MaDatPhong = @id", conn);
            cmd.Parameters.AddWithValue("@id", maDatPhong);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
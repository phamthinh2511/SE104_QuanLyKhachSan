using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyKhachSan_SE104.Migrations
{
    /// <inheritdoc />
    public partial class AddThongTinNhanVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "NhanViens",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "NhanViens",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "NhanViens",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            /*
            migrationBuilder.InsertData(
                table: "NhanViens",
                columns: new[] { "MaNhanVien", "CCCD", "ChucVu", "Email", "HoTen", "SoDienThoai", "TrangThaiLamViec" },
                values: new object[,]
                {
                    { 1, null, true, null, "Quản trị viên", null, true },
                    { 2, null, false, null, "Nhân viên", null, true }
                });

            migrationBuilder.InsertData(
                table: "TaiKhoans",
                columns: new[] { "MaTaiKhoan", "CreatedAt", "MaNhanVien", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "123", "admin" },
                    { 2, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "123", "user" }
                });
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaiKhoans",
                keyColumn: "MaTaiKhoan",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TaiKhoans",
                keyColumn: "MaTaiKhoan",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "NhanViens",
                keyColumn: "MaNhanVien",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "NhanViens",
                keyColumn: "MaNhanVien",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "NhanViens");
        }
    }
}

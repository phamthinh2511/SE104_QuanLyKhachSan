using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan_SE104.Migrations
{
    /// <inheritdoc />
    public partial class AddLichSuCocTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "DichVus",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiCoc",
                table: "DatPhongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LichSuCocs",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaDatPhong = table.Column<int>(type: "int", nullable: false),
                    LoaiGiaoDich = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaDatPhongMoi = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuCocs", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuCocs_DatPhongs_MaDatPhong",
                        column: x => x.MaDatPhong,
                        principalTable: "DatPhongs",
                        principalColumn: "MaDatPhong",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichSuCocs_DatPhongs_MaDatPhongMoi",
                        column: x => x.MaDatPhongMoi,
                        principalTable: "DatPhongs",
                        principalColumn: "MaDatPhong");
                    table.ForeignKey(
                        name: "FK_LichSuCocs_NhanViens_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanViens",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuCocs_MaDatPhong",
                table: "LichSuCocs",
                column: "MaDatPhong");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuCocs_MaDatPhongMoi",
                table: "LichSuCocs",
                column: "MaDatPhongMoi");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuCocs_MaNhanVien",
                table: "LichSuCocs",
                column: "MaNhanVien");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuCocs");

            migrationBuilder.DropColumn(
                name: "TrangThaiCoc",
                table: "DatPhongs");

            migrationBuilder.UpdateData(
                table: "DichVus",
                keyColumn: "MoTa",
                keyValue: null,
                column: "MoTa",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "DichVus",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

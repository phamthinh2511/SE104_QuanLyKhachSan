using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan_SE104.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredRoomSegmentTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoDem",
                table: "ChiTietDatPhongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ThanhTien",
                table: "ChiTietDatPhongs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                @"UPDATE ChiTietDatPhongs
                  SET SoDem = GREATEST(1, CEIL(TIMESTAMPDIFF(DAY, DATE(NgayCheckIn), DATE(NgayCheckOut)))),
                      ThanhTien = GiaDat * GREATEST(1, CEIL(TIMESTAMPDIFF(DAY, DATE(NgayCheckIn), DATE(NgayCheckOut))))
                  WHERE TrangThaiSegment <> 2");

            migrationBuilder.Sql(
                @"UPDATE ChiTietDatPhongs
                  SET SoDem = GREATEST(0, CEIL(TIMESTAMPDIFF(DAY, DATE(NgayCheckIn), DATE(NgayCheckOut)))),
                      ThanhTien = GiaDat * GREATEST(0, CEIL(TIMESTAMPDIFF(DAY, DATE(NgayCheckIn), DATE(NgayCheckOut))))
                  WHERE TrangThaiSegment = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoDem",
                table: "ChiTietDatPhongs");

            migrationBuilder.DropColumn(
                name: "ThanhTien",
                table: "ChiTietDatPhongs");
        }
    }
}

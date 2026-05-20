using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan_SE104.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiSegmentToChiTietDatPhong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrangThaiSegment",
                table: "ChiTietDatPhongs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiSegment",
                table: "ChiTietDatPhongs");
        }
    }
}

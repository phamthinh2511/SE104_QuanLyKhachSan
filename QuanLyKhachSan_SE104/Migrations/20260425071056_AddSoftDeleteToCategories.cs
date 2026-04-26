using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan_SE104.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Phongs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LoaiPhongs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "KhachHangs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DichVus",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Phongs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LoaiPhongs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "KhachHangs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DichVus");
        }
    }
}

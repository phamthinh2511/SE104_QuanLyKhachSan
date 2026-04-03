using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.EntityFrameworkCore;

namespace QuanLyKhachSan_SE104.Model
{
    public class QuanLyKhachSanContext : DbContext
    {
        public QuanLyKhachSanContext() { }
        public QuanLyKhachSanContext(DbContextOptions<QuanLyKhachSanContext> options)
        : base(options) { }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<DatPhong> DatPhongs { get; set; }
        public DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public DbSet<ChiTietDichVu> ChiTietDichVus { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["AzureMySqlConnection"].ConnectionString;
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            catch (Exception ex)
            {
                // Ném lỗi rõ ràng nếu file config thiếu hoặc sai tên connectionString
                throw new Exception("Không tìm thấy chuỗi kết nối trong App.config. " + ex.Message);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TaiKhoan - NhanVien
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.NhanVien)
                .WithMany(n => n.TaiKhoans)
                .HasForeignKey(t => t.MaNhanVien);

            // Phong - LoaiPhong
            modelBuilder.Entity<Phong>()
                .HasOne(p => p.LoaiPhong)
                .WithMany(l => l.Phongs)
                .HasForeignKey(p => p.MaLoaiPhong);

            // DatPhong - KhachHang
            modelBuilder.Entity<DatPhong>()
                .HasOne(d => d.KhachHang)
                .WithMany(k => k.DatPhongs)
                .HasForeignKey(d => d.MaKhachHang);

            // DatPhong - NhanVien
            modelBuilder.Entity<DatPhong>()
                .HasOne(d => d.NhanVien)
                .WithMany(n => n.DatPhongs)
                .HasForeignKey(d => d.MaNhanVien);

            // ChiTietDatPhong
            modelBuilder.Entity<ChiTietDatPhong>()
                .HasOne(c => c.DatPhong)
                .WithMany(d => d.ChiTietDatPhongs)
                .HasForeignKey(c => c.MaDatPhong);

            modelBuilder.Entity<ChiTietDatPhong>()
                .HasOne(c => c.Phong)
                .WithMany(p => p.ChiTietDatPhongs)
                .HasForeignKey(c => c.MaPhong);

            // ChiTietDichVu
            modelBuilder.Entity<ChiTietDichVu>()
                .HasOne(c => c.ChiTietDatPhong)
                .WithMany(ct => ct.ChiTietDichVus)
                .HasForeignKey(c => c.MaChiTietDatPhong);

            modelBuilder.Entity<ChiTietDichVu>()
                .HasOne(c => c.DichVu)
                .WithMany(d => d.ChiTietDichVus)
                .HasForeignKey(c => c.MaDichVu);

            // HoaDon
            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.DatPhong)
                .WithOne(d => d.HoaDon)
                .HasForeignKey<HoaDon>(h => h.MaDatPhong);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.NhanVien)
                .WithMany(n => n.HoaDons)
                .HasForeignKey(h => h.MaNhanVien);
        }
    }
}

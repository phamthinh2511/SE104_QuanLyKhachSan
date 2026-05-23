using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_SE104.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyKhachSan_SE104.DAL
{
    public class DichVuDAL
    {
        /// <summary>
        /// Saves a batch of selected services for one ChiTietDatPhong.
        /// Each call APPENDS new ChiTietDichVu rows — it does NOT delete existing ones,
        /// so staff can call "Thêm dịch vụ" multiple times during a stay.
        /// </summary>
        public void LuuChiTietDichVu(int maChiTietDatPhong, IEnumerable<(int MaDichVu, int SoLuong, decimal DonGia)> items, DateTime thoiGianSuDung)
        {
            if (maChiTietDatPhong <= 0)
                throw new ArgumentException("MaChiTietDatPhong không hợp lệ.");

            var rows = items
                .Where(x => x.SoLuong > 0)
                .Select(x => new ChiTietDichVu
                {
                    MaChiTietDatPhong = maChiTietDatPhong,
                    MaDichVu = x.MaDichVu,
                    SoLuong = x.SoLuong,
                    DonGia = x.DonGia,
                    ThoiGianSuDung = thoiGianSuDung
                })
                .ToList();

            if (rows.Count == 0) return;

            using var ctx = new QuanLyKhachSanContext();
            ctx.ChiTietDichVus.AddRange(rows);
            ctx.SaveChanges();
        }

        /// <summary>
        /// Loads all service detail rows (with DichVu navigation) for a given ChiTietDatPhong.
        /// Used by ChiTietPhongViewModel to refresh after saving.
        /// </summary>
        public List<ChiTietDichVu> LayDichVuTheoChiTiet(int maChiTietDatPhong)
        {
            using var ctx = new QuanLyKhachSanContext();
            return ctx.ChiTietDichVus
                .Include(x => x.DichVu)
                .Where(x => x.MaChiTietDatPhong == maChiTietDatPhong)
                .ToList();
        }

        public List<ChiTietDichVu> LayDichVuTheoMaDatPhong(int maDatPhong)
        {
            using var ctx = new QuanLyKhachSanContext();

            // Tìm tất cả các dịch vụ thuộc về bất kỳ segment nào có chung MaDatPhong
            return ctx.ChiTietDichVus
                .Include(x => x.DichVu)
                .Where(x => ctx.ChiTietDatPhongs
                               .Where(ct => ct.MaDatPhong == maDatPhong)
                               .Select(ct => ct.MaChiTietDatPhong)
                               .Contains(x.MaChiTietDatPhong))
                .ToList();
        }

        /// <summary>
        /// Loads all non-deleted services from the catalogue.
        /// </summary>
        public List<DichVu> LayDanhSachDichVu()
        {
            using var ctx = new QuanLyKhachSanContext();
            return ctx.DichVus
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.LoaiDichVu)
                .ThenBy(d => d.TenDichVu)
                .ToList();
        }
    }
}
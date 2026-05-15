using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan_SE104.Model
{
    /// <summary>
    /// Audit trail for every deposit movement.
    /// Records are NEVER deleted — only status transitions are logged.
    /// </summary>
    public class LichSuCoc
    {
        [Key]
        public int MaLichSu { get; set; }

        /// <summary>Booking this event belongs to.</summary>
        public int MaDatPhong { get; set; }

        /// <summary>
        /// 0 = Thu cọc     (deposit collected at booking)
        /// 1 = Hoàn trả    (Rule 01 - timely cancellation refund)
        /// 2 = Thu doanh thu (Rule 02 - no-show forfeit OR applied at checkout)
        /// 3 = Chuyển booking (Rule 04 - room category change transfer)
        /// </summary>
        public int LoaiGiaoDich { get; set; }

        /// <summary>Amount involved in this transaction.</summary>
        public decimal SoTien { get; set; }

        public DateTime ThoiGian { get; set; }

        /// <summary>Staff who performed this action.</summary>
        public int MaNhanVien { get; set; }

        /// <summary>Optional note or reason.</summary>
        public string GhiChu { get; set; }

        /// <summary>
        /// Only populated for LoaiGiaoDich = 3 (Rule 04 transfer).
        /// Points to the new booking that received the deposit.
        /// </summary>
        public int? MaDatPhongMoi { get; set; }

        // ── Navigation properties ─────────────────────────────────────────
        public DatPhong DatPhong { get; set; }
        public NhanVien NhanVien { get; set; }
    }
}
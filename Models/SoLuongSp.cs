using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_store.Models
{
    public class SoLuongSp
    {
        [Key]
        public int MaSL { get; set; }

        public int MaSP { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string KichThuoc { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string MauSac { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaBan { get; set; }

        public int SoLuongTon { get; set; } = 0;

        [StringLength(500)]
        public string? HinhAnh { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual SanPham? SanPham { get; set; }
    }
}

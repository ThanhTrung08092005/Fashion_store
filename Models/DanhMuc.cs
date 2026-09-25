using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class DanhMuc
    {
        [Key]
        public int MaDM { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDM { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}

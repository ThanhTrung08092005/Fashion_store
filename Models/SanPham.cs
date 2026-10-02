using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class SanPham
    {
        [Key]
        public int MaSP { get; set; }

        public int MaDM { get; set; }

        [Required]
        [StringLength(200)]
        public string TenSP { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [StringLength(100)]
        public string? ChatLieu { get; set; }

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual DanhMuc? DanhMuc { get; set; }
        public virtual ICollection<SoLuongSp> SoLuongSps { get; set; } = new List<SoLuongSp>();
    }
}

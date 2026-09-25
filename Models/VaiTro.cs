using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class VaiTro
    {
        [Key]
        public int MaVaiTro { get; set; }

        [Required]
        [StringLength(50)]
        public string TenVaiTro { get; set; } = string.Empty;

        [StringLength(255)]
        public string? MoTa { get; set; }

        public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
    }
}

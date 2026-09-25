using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class QuanLy
    {
        [Key]
        public int MaQL { get; set; }

        public int MaTK { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        [StringLength(50)]
        public string? ChucVu { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual TaiKhoan? TaiKhoan { get; set; }
    }
}

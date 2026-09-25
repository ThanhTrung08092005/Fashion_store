using System;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class KhachHang
    {
        [Key]
        public int MaKH { get; set; }

        public int MaTK { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(10)]
        public string? GioiTinh { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(500)]
        public string? SoThich { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        public bool DaXoa { get; set; } = false;

        public virtual TaiKhoan? TaiKhoan { get; set; }
    }
}

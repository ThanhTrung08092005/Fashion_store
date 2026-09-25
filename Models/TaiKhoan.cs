using System;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    public class TaiKhoan
    {
        [Key]
        public int MaTK { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } = string.Empty;

        public int MaVaiTro { get; set; }

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual VaiTro? VaiTro { get; set; }
        public virtual QuanLy? QuanLy { get; set; }
        public virtual KhachHang? KhachHang { get; set; }
    }
}

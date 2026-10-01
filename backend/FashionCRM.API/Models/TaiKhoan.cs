namespace FashionCRM.API.Models;

public class TaiKhoan
{
    public int MaTK { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;   // BCrypt hash
    public int MaVaiTro { get; set; }
    public bool TrangThai { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    public VaiTro VaiTro { get; set; } = null!;
    public KhachHang? KhachHang { get; set; }
    public QuanLy? QuanLy { get; set; }
}

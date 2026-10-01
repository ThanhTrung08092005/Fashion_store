namespace FashionCRM.API.Models;

public class QuanLy
{
    public int MaQL { get; set; }
    public int MaTK { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? ChucVu { get; set; }
    public bool TrangThai { get; set; } = true;

    // Navigation
    public TaiKhoan TaiKhoan { get; set; } = null!;
    public ICollection<PhanHoi> PhanHois { get; set; } = new List<PhanHoi>();
    public ICollection<KhaoSat> KhaoSats { get; set; } = new List<KhaoSat>();
    public ICollection<DonHang> DonHangsXuLy { get; set; } = new List<DonHang>();
}

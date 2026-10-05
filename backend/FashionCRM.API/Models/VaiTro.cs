namespace FashionCRM.API.Models;

public class VaiTro
{
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }

    // Navigation
    public ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}

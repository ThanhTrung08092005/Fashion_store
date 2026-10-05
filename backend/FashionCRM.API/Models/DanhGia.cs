namespace FashionCRM.API.Models;

public class DanhGia
{
    public int MaDG { get; set; }
    public int MaKH { get; set; }
    public int MaSP { get; set; }
    public int SoSao { get; set; }
    public string? NoiDung { get; set; }
    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    public KhachHang KhachHang { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}

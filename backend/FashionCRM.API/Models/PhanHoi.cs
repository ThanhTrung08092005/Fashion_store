namespace FashionCRM.API.Models;

public class PhanHoi
{
    public int MaPH { get; set; }
    public int MaKH { get; set; }
    public int? MaSP { get; set; }
    public int? MaQL { get; set; }
    public string? TieuDe { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string? LoaiPhanHoi { get; set; }
    public string TrangThai { get; set; } = "Chưa xử lý";
    public string? PhanHoiQuanLy { get; set; }
    public DateTime NgayGui { get; set; } = DateTime.Now;
    public DateTime? NgayXuLy { get; set; }

    // Navigation
    public KhachHang KhachHang { get; set; } = null!;
    public SanPham? SanPham { get; set; }
    public QuanLy? QuanLy { get; set; }
}

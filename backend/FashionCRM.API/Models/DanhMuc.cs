namespace FashionCRM.API.Models;

public class DanhMuc
{
    public int MaDM { get; set; }
    public string TenDM { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool TrangThai { get; set; } = true;

    public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    public ICollection<KhachHangSoThich> KhachHangSoThiches { get; set; } = new List<KhachHangSoThich>();
}

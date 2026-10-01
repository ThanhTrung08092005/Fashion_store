using System.ComponentModel.DataAnnotations.Schema;

namespace FashionCRM.API.Models;

public class KhachHang
{
    public int MaKH { get; set; }
    public int MaTK { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public string? DiaChi { get; set; }
    public bool DaXoa { get; set; }
    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [NotMapped]
    public string? SoThich
    {
        get => KhachHangSoThiches
            .Select(x => x.DanhMuc?.TenDM)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray()
            is { Length: > 0 } values ? string.Join(", ", values) : null;
        set { }
    }

    // Navigation
    public TaiKhoan TaiKhoan { get; set; } = null!;
    public ICollection<KhachHangSoThich> KhachHangSoThiches { get; set; } = new List<KhachHangSoThich>();
    public ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
    public ICollection<PhanHoi> PhanHois { get; set; } = new List<PhanHoi>();
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
    public ICollection<PhieuKhaoSat> PhieuKhaoSats { get; set; } = new List<PhieuKhaoSat>();
}

public class KhachHangSoThich
{
    public int MaKH { get; set; }
    public int MaDM { get; set; }
    public DateTime NgayChon { get; set; } = DateTime.Now;

    public KhachHang KhachHang { get; set; } = null!;
    public DanhMuc DanhMuc { get; set; } = null!;
}

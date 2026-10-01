using System.ComponentModel.DataAnnotations.Schema;

namespace FashionCRM.API.Models;

public class DonHang
{
    public int MaDH { get; set; }
    public int MaKH { get; set; }
    public int? MaQLXuLy { get; set; }
    public DateTime NgayDat { get; set; } = DateTime.Now;
    public decimal TongTien { get; set; } = 0;
    public string TrangThai { get; set; } = "Chờ xác nhận";
    public string DiaChiGiaoHang { get; set; } = string.Empty;
    public string? SoDienThoaiNhan { get; set; }
    public string? GhiChu { get; set; }

    // Navigation
    public KhachHang KhachHang { get; set; } = null!;
    public QuanLy? QuanLyXuLy { get; set; }
    public ICollection<ChiTietDonHang> ChiTiets { get; set; } = new List<ChiTietDonHang>();
}

public class ChiTietDonHang
{
    public int MaDH { get; set; }
    public int MaSL { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; } = 0;
    public decimal ThanhTien { get; private set; }

    [NotMapped]
    public int MaSP
    {
        get => SoLuongSp?.MaSP ?? 0;
        set
        {
            if (SoLuongSp is not null)
                SoLuongSp.MaSP = value;
        }
    }

    public DonHang DonHang { get; set; } = null!;
    public SoLuongSp SoLuongSp { get; set; } = null!;

    [NotMapped]
    public SanPham? SanPham => SoLuongSp?.SanPham;
}

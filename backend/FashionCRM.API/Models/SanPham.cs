using System.ComponentModel.DataAnnotations.Schema;

namespace FashionCRM.API.Models;

public class SanPham
{
    public int MaSP { get; set; }
    public int MaDM { get; set; }
    public string TenSP { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? ChatLieu { get; set; }
    public bool TrangThai { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [NotMapped]
    public decimal Gia
    {
        get => SoLuongSps.OrderByDescending(x => x.GiaBan).FirstOrDefault()?.GiaBan ?? 0m;
        set
        {
            if (SoLuongSps.Count == 0)
            {
                SoLuongSps.Add(new SoLuongSp
                {
                    SKU = $"AUTO-{Guid.NewGuid():N}",
                    KichThuoc = "M",
                    MauSac = "Đen",
                    GiaBan = value,
                    SoLuongTon = 0,
                    TrangThai = true
                });
                return;
            }

            var item = SoLuongSps.OrderByDescending(x => x.GiaBan).First();
            item.GiaBan = value;
        }
    }

    [NotMapped]
    public int SoLuongTon
    {
        get => SoLuongSps.Sum(x => x.SoLuongTon);
        set
        {
            if (SoLuongSps.Count == 0)
            {
                SoLuongSps.Add(new SoLuongSp
                {
                    SKU = $"AUTO-{Guid.NewGuid():N}",
                    KichThuoc = "M",
                    MauSac = "Đen",
                    GiaBan = 0m,
                    SoLuongTon = value,
                    TrangThai = true
                });
                return;
            }

            var item = SoLuongSps.First();
            item.SoLuongTon = value;
        }
    }

    [NotMapped]
    public string? HinhAnh
    {
        get => SoLuongSps.FirstOrDefault()?.HinhAnh;
        set
        {
            if (SoLuongSps.Count == 0)
            {
                SoLuongSps.Add(new SoLuongSp
                {
                    SKU = $"AUTO-{Guid.NewGuid():N}",
                    KichThuoc = "M",
                    MauSac = "Đen",
                    GiaBan = 0m,
                    SoLuongTon = 0,
                    TrangThai = true,
                    HinhAnh = value
                });
                return;
            }

            SoLuongSps.First().HinhAnh = value;
        }
    }

    [NotMapped]
    public string? KichThuoc
    {
        get => SoLuongSps.FirstOrDefault()?.KichThuoc;
        set
        {
            if (SoLuongSps.Count == 0)
            {
                SoLuongSps.Add(new SoLuongSp
                {
                    SKU = $"AUTO-{Guid.NewGuid():N}",
                    KichThuoc = value ?? "M",
                    MauSac = "Đen",
                    GiaBan = 0m,
                    SoLuongTon = 0,
                    TrangThai = true
                });
                return;
            }

            SoLuongSps.First().KichThuoc = value ?? SoLuongSps.First().KichThuoc;
        }
    }

    [NotMapped]
    public string? MauSac
    {
        get => SoLuongSps.FirstOrDefault()?.MauSac;
        set
        {
            if (SoLuongSps.Count == 0)
            {
                SoLuongSps.Add(new SoLuongSp
                {
                    SKU = $"AUTO-{Guid.NewGuid():N}",
                    KichThuoc = "M",
                    MauSac = value ?? "Đen",
                    GiaBan = 0m,
                    SoLuongTon = 0,
                    TrangThai = true
                });
                return;
            }

            SoLuongSps.First().MauSac = value ?? SoLuongSps.First().MauSac;
        }
    }

    // Navigation
    public DanhMuc DanhMuc { get; set; } = null!;
    public ICollection<SoLuongSp> SoLuongSps { get; set; } = new List<SoLuongSp>();
    public ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
    public ICollection<PhanHoi> PhanHois { get; set; } = new List<PhanHoi>();
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
}

public class SoLuongSp
{
    public int MaSL { get; set; }
    public int MaSP { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string KichThuoc { get; set; } = string.Empty;
    public string MauSac { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public int SoLuongTon { get; set; }
    public string? HinhAnh { get; set; }
    public bool TrangThai { get; set; } = true;

    [NotMapped]
    public decimal Gia => GiaBan;

    public SanPham SanPham { get; set; } = null!;
    public ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
}

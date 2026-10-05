namespace FashionCRM.API.Models;

public class KhaoSat
{
    public int MaKS { get; set; }
    public int MaQL { get; set; }
    public string TenKhaoSat { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public DateOnly NgayBatDau { get; set; }
    public DateOnly NgayKetThuc { get; set; }
    public bool TrangThai { get; set; } = true;

    public QuanLy QuanLy { get; set; } = null!;
    public ICollection<CauHoiKhaoSat> CauHois { get; set; } = new List<CauHoiKhaoSat>();
    public ICollection<PhieuKhaoSat> PhieuKhaoSats { get; set; } = new List<PhieuKhaoSat>();
}

public class CauHoiKhaoSat
{
    public int MaCauHoi { get; set; }
    public int MaKS { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public int ThuTu { get; set; } = 1;
    public bool BatBuoc { get; set; } = false;

    public KhaoSat KhaoSat { get; set; } = null!;
    public ICollection<LuaChonCauHoi> LuaChons { get; set; } = new List<LuaChonCauHoi>();
    public ICollection<TraLoiKhaoSat> TraLois { get; set; } = new List<TraLoiKhaoSat>();
}

public class LuaChonCauHoi
{
    public int MaLuaChon { get; set; }
    public int MaCauHoi { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int ThuTu { get; set; } = 1;

    public CauHoiKhaoSat CauHoi { get; set; } = null!;
}

public class PhieuKhaoSat
{
    public int MaPhieuKS { get; set; }
    public int MaKS { get; set; }
    public int MaKH { get; set; }
    public DateTime NgayBatDau { get; set; } = DateTime.Now;
    public DateTime? NgayNop { get; set; }
    public string TrangThai { get; set; } = "Đang làm";

    public KhaoSat KhaoSat { get; set; } = null!;
    public KhachHang KhachHang { get; set; } = null!;
    public ICollection<TraLoiKhaoSat> TraLois { get; set; } = new List<TraLoiKhaoSat>();
}

public class TraLoiKhaoSat
{
    public int MaTraLoi { get; set; }
    public int MaPhieuKS { get; set; }
    public int MaCauHoi { get; set; }
    public int? MaLuaChon { get; set; }
    public string? NoiDungTraLoi { get; set; }
    public int? SoSao { get; set; }

    public PhieuKhaoSat PhieuKhaoSat { get; set; } = null!;
    public CauHoiKhaoSat CauHoi { get; set; } = null!;
    public LuaChonCauHoi? LuaChon { get; set; }
}

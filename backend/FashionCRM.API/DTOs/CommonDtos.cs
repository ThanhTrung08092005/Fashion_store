namespace FashionCRM.API.DTOs;

// ── Pagination wrapper ────────────────────────────────────────────────────────
public class PagedResult<T>
{
    public IEnumerable<T> Items  { get; set; } = Enumerable.Empty<T>();
    public int TotalItems        { get; set; }
    public int Page              { get; set; }
    public int PageSize          { get; set; }
    public int TotalPages        => (int)Math.Ceiling((double)TotalItems / PageSize);
}

// ── API response envelope ─────────────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool Success    { get; set; }
    public string Message  { get; set; } = string.Empty;
    public T? Data         { get; set; }

    public static ApiResponse<T> Ok(T data, string msg = "Thành công")
        => new() { Success = true, Message = msg, Data = data };

    public static ApiResponse<T> Fail(string msg)
        => new() { Success = false, Message = msg };
}

// ── Query params ─────────────────────────────────────────────────────────────
public class QueryParams
{
    public int    Page     { get; set; } = 1;
    public int    PageSize { get; set; } = 10;
    public string? Search  { get; set; }
    public string? SortBy  { get; set; }
    public string  SortDir { get; set; } = "desc";
}

// ── KhachHang DTOs ────────────────────────────────────────────────────────────
public record KhachHangDto(
    int MaKH, string HoTen, string Email, string? SoDienThoai,
    DateOnly? NgaySinh, string? GioiTinh, string? DiaChi, string? SoThich,
    DateTime NgayDangKy, bool TrangThaiTK
);

public record KhachHangCreateDto(
    int MaTK, string HoTen, string Email, string? SoDienThoai,
    DateOnly? NgaySinh, string? GioiTinh, string? DiaChi, string? SoThich
);

public record KhachHangCreateWithAccountDto(
    string TenDangNhap, string MatKhau, string HoTen, string Email,
    string? SoDienThoai, DateOnly? NgaySinh, string? GioiTinh,
    string? DiaChi, string? SoThich
);

public record KhachHangUpdateDto(
    string HoTen, string Email, string? SoDienThoai,
    DateOnly? NgaySinh, string? GioiTinh, string? DiaChi, string? SoThich
);

// ── SanPham DTOs ──────────────────────────────────────────────────────────────
public record SanPhamDto(
    int MaSP, int MaDM, string TenDM, string TenSP, decimal Gia,
    int SoLuongTon, string? MoTa, string? HinhAnh,
    string? KichThuoc, string? MauSac, string? ChatLieu,
    bool TrangThai, DateTime NgayTao
);

public record SanPhamCreateDto(
    int MaDM, string TenSP, decimal Gia, int SoLuongTon,
    string? MoTa, string? HinhAnh, string? KichThuoc,
    string? MauSac, string? ChatLieu
);

public record SanPhamUpdateDto(
    int MaDM, string TenSP, decimal Gia, int SoLuongTon,
    string? MoTa, string? HinhAnh, string? KichThuoc,
    string? MauSac, string? ChatLieu, bool TrangThai
);

// ── DanhMuc DTOs ──────────────────────────────────────────────────────────────
public record DanhMucDto(int MaDM, string TenDM, string? MoTa, bool TrangThai, int SoSanPham);
public record DanhMucCreateDto(string TenDM, string? MoTa);

// ── DonHang DTOs ──────────────────────────────────────────────────────────────
public record DonHangDto(
    int MaDH, int MaKH, string HoTenKH, DateTime NgayDat,
    decimal TongTien, string TrangThai, string DiaChiGiaoHang,
    string? SoDienThoaiNhan, List<ChiTietDonHangDto> ChiTiets
);
public record ChiTietDonHangDto(int MaSP, string TenSP, int SoLuong, decimal DonGia, decimal ThanhTien);
public record DonHangCreateDto(
    int MaKH, string DiaChiGiaoHang, string? SoDienThoaiNhan,
    List<ChiTietCreateDto> ChiTiets
);
public record ChiTietCreateDto(int MaSP, int SoLuong);
public record UpdateTrangThaiDonHangDto(string TrangThai);

// ── PhanHoi DTOs ──────────────────────────────────────────────────────────────
public record PhanHoiDto(
    int MaPH, int MaKH, string HoTenKH, int? MaSP, string? TenSP,
    string? TieuDe, string NoiDung, string? LoaiPhanHoi,
    string TrangThai, string? PhanHoiQuanLy,
    DateTime NgayGui, DateTime? NgayXuLy
);
public record PhanHoiCreateDto(int MaKH, int? MaSP, string? TieuDe, string NoiDung, string? LoaiPhanHoi);
public record PhanHoiReplyDto(int MaQL, string PhanHoiQuanLy, string TrangThai);

// ── DanhGia DTOs ──────────────────────────────────────────────────────────────
public record DanhGiaDto(int MaDG, int MaKH, string HoTenKH, int MaSP, int SoSao, string? NoiDung, DateTime NgayDanhGia);
public record DanhGiaCreateDto(int MaKH, int MaSP, int SoSao, string? NoiDung);

// ── KhaoSat DTOs ──────────────────────────────────────────────────────────────
public record KhaoSatDto(
    int MaKS, int MaQL, string TenKhaoSat, string? MoTa,
    DateOnly NgayBatDau, DateOnly NgayKetThuc, bool TrangThai,
    int SoCauHoi, int SoPhieuHoanThanh
);
public record KhaoSatCreateDto(
    int MaQL, string TenKhaoSat, string? MoTa,
    DateOnly NgayBatDau, DateOnly NgayKetThuc,
    List<CauHoiCreateDto> CauHois
);
public record CauHoiCreateDto(
    string NoiDung, string LoaiCauHoi, int ThuTu, bool BatBuoc,
    List<string>? LuaChons
);

// ── TaiKhoan DTOs ─────────────────────────────────────────────────────────────
public record TaiKhoanDto(int MaTK, string TenDangNhap, string TenVaiTro, bool TrangThai, DateTime NgayTao);
public record TaiKhoanCreateDto(string TenDangNhap, string MatKhau, int MaVaiTro);
public record TaiKhoanUpdateDto(int MaVaiTro, bool TrangThai);
public record QuanLyAccountCreateDto(
    string TenDangNhap, string MatKhau, string HoTen, string? Email,
    string? SoDienThoai, string? ChucVu
);

// ── QuanLy DTOs ───────────────────────────────────────────────────────────────
public record QuanLyDto(int MaQL, string TenDangNhap, string HoTen, string? Email, string? SoDienThoai, string? ChucVu, bool TrangThai);
public record QuanLyCreateDto(int MaTK, string HoTen, string? Email, string? SoDienThoai, string? ChucVu);

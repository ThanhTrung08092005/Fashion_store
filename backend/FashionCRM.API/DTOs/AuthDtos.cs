namespace FashionCRM.API.DTOs;

// ── Request ──────────────────────────────────────────────────────────────────
public record LoginRequest(string TenDangNhap, string MatKhau);

public record RegisterRequest(
    string TenDangNhap,
    string MatKhau,
    string HoTen,
    string Email,
    string? SoDienThoai,
    DateOnly? NgaySinh,
    string? GioiTinh,
    string? DiaChi,
    string? SoThich
);

public record ChangePasswordRequest(string MatKhauCu, string MatKhauMoi);

// ── Response ─────────────────────────────────────────────────────────────────
public record LoginResponse(
    string Token,
    int MaTK,
    string TenDangNhap,
    string HoTen,
    string VaiTro,
    string? Email
);

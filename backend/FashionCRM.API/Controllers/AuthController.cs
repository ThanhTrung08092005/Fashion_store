using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Helpers;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtHelper    _jwt;

    public AuthController(AppDbContext db, JwtHelper jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var tk = await _db.TaiKhoans
            .Include(t => t.VaiTro)
            .Include(t => t.KhachHang)
            .Include(t => t.QuanLy)
            .FirstOrDefaultAsync(t => t.TenDangNhap == req.TenDangNhap);

        if (tk is null || !BCrypt.Net.BCrypt.Verify(req.MatKhau, tk.MatKhau))
            return Unauthorized(ApiResponse<object>.Fail("Tên đăng nhập hoặc mật khẩu không đúng."));

        if (!tk.TrangThai)
            return Unauthorized(ApiResponse<object>.Fail("Tài khoản đã bị khóa."));

        string hoTen = tk.KhachHang?.HoTen ?? tk.QuanLy?.HoTen ?? tk.TenDangNhap;
        string? email = tk.KhachHang?.Email ?? tk.QuanLy?.Email;
        string token  = _jwt.GenerateToken(tk, hoTen, email);

        var resp = new LoginResponse(token, tk.MaTK, tk.TenDangNhap, hoTen, tk.VaiTro.TenVaiTro, email);
        return Ok(ApiResponse<LoginResponse>.Ok(resp, "Đăng nhập thành công!"));
    }

    // POST /api/auth/register  (Khách hàng tự đăng ký)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var username = req.TenDangNhap?.Trim();
        var fullName = req.HoTen?.Trim();
        var email = req.Email?.Trim();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 4 || username.Length > 50)
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập phải có từ 4 đến 50 ký tự."));

        if (string.IsNullOrWhiteSpace(req.MatKhau) || req.MatKhau.Length < 8)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu phải có ít nhất 8 ký tự."));

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
            return BadRequest(ApiResponse<object>.Fail("Họ tên là bắt buộc và không quá 100 ký tự."));

        if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(ApiResponse<object>.Fail("Email không hợp lệ."));

        if (req.NgaySinh > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(ApiResponse<object>.Fail("Ngày sinh không thể nằm trong tương lai."));

        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        if (await _db.KhachHangs.AnyAsync(k => k.Email == email))
            return BadRequest(ApiResponse<object>.Fail("Email đã được sử dụng."));

        var vaiTroKH = await _db.VaiTros
            .FirstOrDefaultAsync(v =>
                v.TenVaiTro == "KhachHang" ||
                v.TenVaiTro == "Khách hàng" ||
                v.TenVaiTro.Contains("Khách"));

        if (vaiTroKH is null)
            return BadRequest(ApiResponse<object>.Fail("Không tìm thấy vai trò khách hàng trong cơ sở dữ liệu."));

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var tk = new TaiKhoan
        {
            TenDangNhap = username,
            MatKhau     = BCrypt.Net.BCrypt.HashPassword(req.MatKhau),
            MaVaiTro    = vaiTroKH.MaVaiTro,
            TrangThai   = true,
            NgayTao     = DateTime.Now
        };
        _db.TaiKhoans.Add(tk);
        await _db.SaveChangesAsync();

        var kh = new KhachHang
        {
            MaTK        = tk.MaTK,
            HoTen       = fullName,
            Email       = email,
            SoDienThoai = req.SoDienThoai,
            NgaySinh    = req.NgaySinh,
            GioiTinh    = req.GioiTinh,
            DiaChi      = req.DiaChi,
            SoThich     = req.SoThich,
            NgayDangKy  = DateTime.Now
        };
        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();

        var interestNames = (req.SoThich ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (interestNames.Length > 0)
        {
            var categories = await _db.DanhMucs
                .Where(d => interestNames.Contains(d.TenDM))
                .ToListAsync();
            _db.KhachHangSoThiches.AddRange(categories.Select(category => new KhachHangSoThich
            {
                MaKH = kh.MaKH,
                MaDM = category.MaDM
            }));
            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        // Reload with navigation
        await _db.Entry(tk).Reference(t => t.VaiTro).LoadAsync();
        string token = _jwt.GenerateToken(tk, kh.HoTen, kh.Email);
        var resp = new LoginResponse(token, tk.MaTK, tk.TenDangNhap, kh.HoTen, "KhachHang", kh.Email);
        return Ok(ApiResponse<LoginResponse>.Ok(resp, "Đăng ký thành công!"));
    }

    // POST /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var tk   = await _db.TaiKhoans.FindAsync(maTK);
        if (tk is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.MatKhauCu, tk.MatKhau))
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu hiện tại không đúng."));

        tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đổi mật khẩu thành công."));
    }
}

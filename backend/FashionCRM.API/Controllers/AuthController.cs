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

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability([FromQuery] string? username, [FromQuery] string? email)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            var value = username.Trim();
            if (value.Length < 4 || value.Length > 50 || value.Any(char.IsWhiteSpace))
                return Ok(ApiResponse<object>.Ok(new { available = false, field = "username", reason = "Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng." }));
            var exists = await _db.TaiKhoans
                .Include(a => a.KhachHang)
                .Include(a => a.QuanLy)
                .AnyAsync(account => account.TenDangNhap.ToLower() == value.ToLower()
                    && !(account.KhachHang != null && account.KhachHang.DaXoa)
                    && !(account.MaVaiTro == 1 && !account.TrangThai && (account.QuanLy == null || !account.QuanLy.TrangThai)));
            return Ok(ApiResponse<object>.Ok(new { available = !exists, field = "username", reason = exists ? "Tên đăng nhập đã tồn tại." : null }));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var value = email.Trim();
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value))
                return Ok(ApiResponse<object>.Ok(new { available = false, field = "email", reason = "Email không hợp lệ." }));
            var exists = await _db.KhachHangs.AnyAsync(customer => customer.Email.ToLower() == value.ToLower() && !customer.DaXoa);
            return Ok(ApiResponse<object>.Ok(new { available = !exists, field = "email", reason = exists ? "Email đã được sử dụng." : null }));
        }

        return BadRequest(ApiResponse<object>.Fail("Cần cung cấp tên đăng nhập hoặc email để kiểm tra."));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenDangNhap) || req.TenDangNhap.Any(char.IsWhiteSpace))
            return BadRequest(ApiResponse<object>.Fail("Vui lòng nhập tên đăng nhập hợp lệ."));

        var tk = await _db.TaiKhoans
            .Include(t => t.VaiTro)
            .Include(t => t.KhachHang)
            .Include(t => t.QuanLy)
            .FirstOrDefaultAsync(t => t.TenDangNhap == req.TenDangNhap);

        if (tk is null)
            return Unauthorized(ApiResponse<object>.Fail("Tên đăng nhập không tồn tại trong hệ thống."));

        var customer = tk.KhachHang;
        if (tk.MaVaiTro != 2 || customer is null)
            return Unauthorized(ApiResponse<object>.Fail("Tài khoản Admin hoặc Quản lý không thể đăng nhập tại trang khách hàng."));

        if (!tk.TrangThai || customer.DaXoa)
            return Unauthorized(ApiResponse<object>.Fail("Tài khoản đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ."));

        bool passwordMatches;
        if (tk.MatKhau.StartsWith("$2", StringComparison.Ordinal))
        {
            try { passwordMatches = BCrypt.Net.BCrypt.Verify(req.MatKhau, tk.MatKhau); }
            catch { passwordMatches = false; }
        }
        else
        {
            passwordMatches = string.Equals(req.MatKhau, tk.MatKhau, StringComparison.Ordinal);
            if (passwordMatches) tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhau);
        }

        if (!passwordMatches)
            return Unauthorized(ApiResponse<object>.Fail("Mật khẩu không chính xác. Vui lòng kiểm tra lại."));

        await _db.SaveChangesAsync();

        string hoTen = customer.HoTen;
        string? email = customer.Email;
        string token  = _jwt.GenerateToken(tk, hoTen, email);

        bool hasPreferences = await _db.KhachHangSoThiches.AnyAsync(x => x.MaKH == customer.MaKH);
        var resp = new LoginResponse(token, tk.MaTK, tk.TenDangNhap, hoTen, tk.VaiTro.TenVaiTro, email, hasPreferences);
        return Ok(ApiResponse<LoginResponse>.Ok(resp, "Đăng nhập thành công!"));
    }

    // POST /api/auth/register  (Khách hàng tự đăng ký)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var username = req.TenDangNhap?.Trim();
        var fullName = req.HoTen?.Trim();
        var email = req.Email?.Trim();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 4 || username.Length > 50 || username.Any(char.IsWhiteSpace))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng."));

        if (string.IsNullOrWhiteSpace(req.MatKhau) || req.MatKhau.Length < 8)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu phải có ít nhất 8 ký tự."));

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
            return BadRequest(ApiResponse<object>.Fail("Họ tên là bắt buộc và không quá 100 ký tự."));

        if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(ApiResponse<object>.Fail("Email không hợp lệ."));

        if (!string.IsNullOrWhiteSpace(req.SoDienThoai)
            && (req.SoDienThoai.Length > 15 || req.SoDienThoai.Any(character => !char.IsDigit(character))))
            return BadRequest(ApiResponse<object>.Fail("Số điện thoại không đúng định dạng."));

        if (req.NgaySinh > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(ApiResponse<object>.Fail("Ngày sinh không thể nằm trong tương lai."));

        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var deletedAccs = await _db.TaiKhoans
            .Include(t => t.KhachHang)
            .Include(t => t.QuanLy)
            .Where(t => t.TenDangNhap.ToLower() == username.ToLower()
                && ((t.KhachHang != null && t.KhachHang.DaXoa)
                    || (t.MaVaiTro == 1 && !t.TrangThai && (t.QuanLy == null || !t.QuanLy.TrangThai))))
            .ToListAsync();
        foreach (var acc in deletedAccs)
        {
            acc.TenDangNhap = $"del_{acc.MaTK}_{stamp}";
            if (acc.KhachHang != null && acc.KhachHang.DaXoa && !acc.KhachHang.Email.StartsWith("del_"))
                acc.KhachHang.Email = $"del_{acc.KhachHang.MaKH}_{stamp}@deleted.local";
        }
        var deletedCusts = await _db.KhachHangs
            .Include(k => k.TaiKhoan)
            .Where(k => k.Email.ToLower() == email.ToLower() && k.DaXoa)
            .ToListAsync();
        foreach (var khItem in deletedCusts)
        {
            khItem.Email = $"del_{khItem.MaKH}_{stamp}@deleted.local";
            if (khItem.TaiKhoan != null && !khItem.TaiKhoan.TenDangNhap.StartsWith("del_"))
                khItem.TaiKhoan.TenDangNhap = $"del_{khItem.TaiKhoan.MaTK}_{stamp}";
        }
        if (deletedAccs.Count > 0 || deletedCusts.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap.ToLower() == username.ToLower()))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        if (await _db.KhachHangs.AnyAsync(k => k.Email.ToLower() == email.ToLower() && !k.DaXoa))
            return BadRequest(ApiResponse<object>.Fail("Email đã được sử dụng."));

        var selectedIds = (req.MaDMs ?? new List<int>()).Distinct().ToList();
        var categories = await _db.DanhMucs
            .Where(category => selectedIds.Contains(category.MaDM) && category.TrangThai)
            .ToListAsync();
        if (categories.Count != selectedIds.Count)
            return BadRequest(ApiResponse<object>.Fail("Có sở thích không còn hợp lệ trong danh mục hiện tại."));

        var vaiTroKH = await _db.VaiTros.FirstOrDefaultAsync(v => v.MaVaiTro == 2);

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
            NgayDangKy  = DateTime.Now
        };
        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();

        if (categories.Count > 0)
        {
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
        bool hasPreferences = await _db.KhachHangSoThiches.AnyAsync(x => x.MaKH == kh.MaKH);
        var resp = new LoginResponse(token, tk.MaTK, tk.TenDangNhap, kh.HoTen, "KhachHang", kh.Email, hasPreferences);
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

        if (string.IsNullOrWhiteSpace(req.MatKhauCu))
            return BadRequest(ApiResponse<object>.Fail("Vui lòng nhập mật khẩu hiện tại."));

        if (string.IsNullOrWhiteSpace(req.MatKhauMoi) || req.MatKhauMoi.Length < 8)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới phải có tối thiểu 8 ký tự."));

        bool currentMatches = tk.MatKhau.StartsWith("$2", StringComparison.Ordinal)
            ? BCrypt.Net.BCrypt.Verify(req.MatKhauCu, tk.MatKhau)
            : string.Equals(req.MatKhauCu, tk.MatKhau, StringComparison.Ordinal);

        if (!currentMatches)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu hiện tại không đúng."));

        bool sameAsOld = req.MatKhauCu == req.MatKhauMoi
            || (tk.MatKhau.StartsWith("$2", StringComparison.Ordinal)
                ? BCrypt.Net.BCrypt.Verify(req.MatKhauMoi, tk.MatKhau)
                : string.Equals(req.MatKhauMoi, tk.MatKhau, StringComparison.Ordinal));

        if (sameAsOld)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới phải khác mật khẩu hiện tại."));

        tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đổi mật khẩu thành công."));
    }
}

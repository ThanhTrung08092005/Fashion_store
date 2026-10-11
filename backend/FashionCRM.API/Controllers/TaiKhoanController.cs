using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/taikhoan")]
[Authorize(Roles = "Admin")]
public class TaiKhoanController : ControllerBase
{
    private readonly AppDbContext _db;
    public TaiKhoanController(AppDbContext db) => _db = db;

    // GET /api/taikhoan
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] QueryParams q,
        [FromQuery] int? maVaiTro,
        [FromQuery] bool? trangThai)
    {
        var query = _db.TaiKhoans.Include(t => t.VaiTro).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(t => t.TenDangNhap.Contains(q.Search));
        if (maVaiTro.HasValue)   query = query.Where(t => t.MaVaiTro == maVaiTro);
        if (trangThai.HasValue)  query = query.Where(t => t.TrangThai == trangThai);

        query = q.SortBy switch
        {
            "tenDangNhap" => q.SortDir == "asc"
                ? query.OrderBy(t => t.TenDangNhap)
                : query.OrderByDescending(t => t.TenDangNhap),
            _ => query.OrderByDescending(t => t.NgayTao)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(t => new TaiKhoanDto(t.MaTK, t.TenDangNhap, t.VaiTro.TenVaiTro, t.TrangThai, t.NgayTao))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<TaiKhoanDto>>.Ok(new PagedResult<TaiKhoanDto>
        {
            Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize
        }));
    }

    // GET /api/taikhoan/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _db.TaiKhoans.Include(x => x.VaiTro).FirstOrDefaultAsync(x => x.MaTK == id);
        if (t is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy tài khoản."));
        return Ok(ApiResponse<TaiKhoanDto>.Ok(new TaiKhoanDto(t.MaTK, t.TenDangNhap, t.VaiTro.TenVaiTro, t.TrangThai, t.NgayTao)));
    }

    // POST /api/taikhoan
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaiKhoanCreateDto dto)
    {
        if (dto.MaVaiTro is not (1 or 2))
            return BadRequest(ApiResponse<object>.Fail("Chỉ được tạo tài khoản khách hàng hoặc quản lý."));
        var username = dto.TenDangNhap?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50 || username.Any(char.IsWhiteSpace))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập phải có tối đa 50 ký tự và không chứa khoảng trắng."));
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        var tk = new TaiKhoan
        {
            TenDangNhap = username,
            MatKhau     = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
            MaVaiTro    = dto.MaVaiTro,
            TrangThai   = true,
            NgayTao     = DateTime.Now
        };
        _db.TaiKhoans.Add(tk);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { tk.MaTK }, "Tạo tài khoản thành công."));
    }

    // POST /api/taikhoan/manager - Admin tạo tài khoản nội bộ cho quản lý
    [HttpPost("manager")]
    public async Task<IActionResult> CreateManager([FromBody] QuanLyAccountCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || dto.TenDangNhap.Any(char.IsWhiteSpace)
            || dto.TenDangNhap.Trim().Length > 50 || string.IsNullOrWhiteSpace(dto.MatKhau) ||
            string.IsNullOrWhiteSpace(dto.HoTen))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập không được chứa khoảng trắng; mật khẩu và họ tên là bắt buộc."));

        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaVaiTro = 1,
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            _db.QuanLys.Add(new QuanLy
            {
                MaTK = taiKhoan.MaTK,
                HoTen = dto.HoTen.Trim(),
                Email = dto.Email,
                SoDienThoai = dto.SoDienThoai,
                ChucVu = dto.ChucVu,
                TrangThai = true
            });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.Ok(new { taiKhoan.MaTK }, "Tạo tài khoản quản lý thành công."));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT /api/taikhoan/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TaiKhoanUpdateDto dto)
    {
        var tk = await _db.TaiKhoans
            .Include(account => account.KhachHang)
            .Include(account => account.QuanLy)
            .FirstOrDefaultAsync(account => account.MaTK == id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        if (tk.MaVaiTro == 3)
            return BadRequest(ApiResponse<object>.Fail("Không thể thay đổi tài khoản Admin hệ thống."));
        if (dto.MaVaiTro is not (1 or 2))
            return BadRequest(ApiResponse<object>.Fail("Vai trò chỉ được chuyển giữa Khách hàng và Quản lý."));
        var previousRole = tk.MaVaiTro;

        if (previousRole == 1 && dto.MaVaiTro == 2)
        {
            var birthDate = tk.QuanLy?.NgaySinh ?? tk.KhachHang?.NgaySinh;
            var gender = tk.QuanLy?.GioiTinh ?? tk.KhachHang?.GioiTinh;
            var latestAllowedBirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-16).AddDays(-1));
            if (!birthDate.HasValue || birthDate.Value > latestAllowedBirthDate
                || !new[] { "Nam", "Nữ", "Khác" }.Contains(gender, StringComparer.Ordinal))
                return BadRequest(ApiResponse<object>.Fail("Cần cập nhật ngày sinh hợp lệ và giới tính trong hồ sơ quản lý trước khi chuyển thành khách hàng."));
        }

        if (dto.MaVaiTro == 1 && tk.QuanLy is null)
        {
            if (tk.KhachHang is null)
                return BadRequest(ApiResponse<object>.Fail("Tài khoản không có hồ sơ để chuyển thành Quản lý."));
            _db.QuanLys.Add(new QuanLy
            {
                MaTK = tk.MaTK,
                HoTen = tk.KhachHang.HoTen,
                Email = tk.KhachHang.Email,
                SoDienThoai = tk.KhachHang.SoDienThoai,
                NgaySinh = tk.KhachHang.NgaySinh,
                GioiTinh = tk.KhachHang.GioiTinh,
                ChucVu = "Quản lý cửa hàng",
                TrangThai = true
            });
        }
        else if (dto.MaVaiTro == 1 && tk.QuanLy is not null && tk.KhachHang is not null)
        {
            tk.QuanLy.NgaySinh = tk.KhachHang.NgaySinh;
            tk.QuanLy.GioiTinh = tk.KhachHang.GioiTinh;
        }
        else if (dto.MaVaiTro == 2 && tk.KhachHang is null)
        {
            var manager = tk.QuanLy;
            if (manager is null || string.IsNullOrWhiteSpace(manager.Email))
                return BadRequest(ApiResponse<object>.Fail("Quản lý cần có email hợp lệ trước khi chuyển thành khách hàng."));
            if (await _db.KhachHangs.AnyAsync(customer => customer.Email == manager.Email))
                return BadRequest(ApiResponse<object>.Fail("Email hồ sơ quản lý đã được một khách hàng khác sử dụng."));
            _db.KhachHangs.Add(new KhachHang
            {
                MaTK = tk.MaTK,
                HoTen = manager.HoTen,
                Email = manager.Email,
                SoDienThoai = manager.SoDienThoai,
                NgaySinh = manager.NgaySinh,
                GioiTinh = manager.GioiTinh,
                NgayDangKy = tk.NgayTao,
                DaXoa = false
            });
        }

        if (previousRole == 2 && dto.MaVaiTro == 1 && tk.KhachHang is not null && tk.QuanLy is not null)
        {
            tk.QuanLy.NgaySinh = tk.KhachHang.NgaySinh;
            tk.QuanLy.GioiTinh = tk.KhachHang.GioiTinh;
        }
        else if (previousRole == 1 && dto.MaVaiTro == 2 && tk.QuanLy is not null && tk.KhachHang is not null)
        {
            tk.KhachHang.NgaySinh = tk.QuanLy.NgaySinh ?? tk.KhachHang.NgaySinh;
            tk.KhachHang.GioiTinh = tk.QuanLy.GioiTinh ?? tk.KhachHang.GioiTinh;
        }

        tk.MaVaiTro  = dto.MaVaiTro;
        tk.TrangThai = dto.TrangThai;
        if (tk.QuanLy is not null)
            tk.QuanLy.TrangThai = dto.MaVaiTro == 1 && dto.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    // DELETE /api/taikhoan/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tk = await _db.TaiKhoans
            .Include(account => account.KhachHang)
            .Include(account => account.QuanLy)
            .FirstOrDefaultAsync(account => account.MaTK == id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        if (tk.MaVaiTro == 3)
            return BadRequest(ApiResponse<object>.Fail("Không thể xóa tài khoản Admin hệ thống."));
        if (tk.KhachHang is not null)
        {
            tk.KhachHang.DaXoa = true;
            tk.TrangThai = false;
        }
        else if (tk.QuanLy is not null)
        {
            tk.QuanLy.TrangThai = false;
            tk.TrangThai = false;
        }
        else
        {
            _db.TaiKhoans.Remove(tk);
        }
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa mềm và khóa tài khoản."));
    }

    // PATCH /api/taikhoan/{id}/lock
    [HttpPatch("{id:int}/lock")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        if (tk.MaVaiTro == 3)
            return BadRequest(ApiResponse<object>.Fail("Không thể khóa tài khoản Admin hệ thống."));
        tk.TrangThai = !tk.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { tk.TrangThai },
            tk.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản."));
    }
}

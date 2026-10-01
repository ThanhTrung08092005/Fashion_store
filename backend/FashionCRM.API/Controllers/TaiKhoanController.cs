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
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        var tk = new TaiKhoan
        {
            TenDangNhap = dto.TenDangNhap,
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
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau) ||
            string.IsNullOrWhiteSpace(dto.HoTen))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập, mật khẩu và họ tên là bắt buộc."));

        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaVaiTro = 2,
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
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        tk.MaVaiTro  = dto.MaVaiTro;
        tk.TrangThai = dto.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    // DELETE /api/taikhoan/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        _db.TaiKhoans.Remove(tk);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa tài khoản."));
    }

    // PATCH /api/taikhoan/{id}/lock
    [HttpPatch("{id:int}/lock")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var tk = await _db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        tk.TrangThai = !tk.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { tk.TrangThai },
            tk.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản."));
    }
}

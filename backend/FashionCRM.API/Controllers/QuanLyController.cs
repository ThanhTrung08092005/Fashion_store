using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/quanly")]
[Authorize(Roles = "Admin")]
public class QuanLyController : ControllerBase
{
    private readonly AppDbContext _db;
    public QuanLyController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q)
    {
        var query = _db.QuanLys.Include(ql => ql.TaiKhoan).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(ql => ql.HoTen.Contains(q.Search) ||
                (ql.Email != null && ql.Email.Contains(q.Search)));

        var total = await query.CountAsync();
        var items = await query.OrderBy(ql => ql.HoTen)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(ql => new QuanLyDto(ql.MaQL, ql.TaiKhoan.TenDangNhap,
                ql.HoTen, ql.Email, ql.SoDienThoai, ql.ChucVu, ql.TrangThai))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<QuanLyDto>>.Ok(new PagedResult<QuanLyDto>
        {
            Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuanLyCreateDto dto)
    {
        var ql = new QuanLy
        {
            MaTK = dto.MaTK, HoTen = dto.HoTen, Email = dto.Email,
            SoDienThoai = dto.SoDienThoai, ChucVu = dto.ChucVu, TrangThai = true
        };
        _db.QuanLys.Add(ql);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { ql.MaQL }, "Thêm quản lý viên thành công."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] QuanLyCreateDto dto)
    {
        var ql = await _db.QuanLys.FindAsync(id);
        if (ql is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        ql.HoTen = dto.HoTen; ql.Email = dto.Email;
        ql.SoDienThoai = dto.SoDienThoai; ql.ChucVu = dto.ChucVu;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ql = await _db.QuanLys.FindAsync(id);
        if (ql is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        _db.QuanLys.Remove(ql);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa quản lý viên."));
    }
}

using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/danhmuc")]
public class DanhMucController : ControllerBase
{
    private readonly AppDbContext _db;
    public DanhMucController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? trangThai)
    {
        var query = _db.DanhMucs.Include(d => d.SanPhams).AsQueryable();
        if (trangThai.HasValue) query = query.Where(d => d.TrangThai == trangThai);

        var items = await query.OrderBy(d => d.MaDM)
            .Select(d => new DanhMucDto(d.MaDM, d.TenDM, d.MoTa, d.TrangThai,
                d.SanPhams.Count(s => s.TrangThai)))
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<DanhMucDto>>.Ok(items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var d = await _db.DanhMucs.Include(x => x.SanPhams).FirstOrDefaultAsync(x => x.MaDM == id);
        if (d is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy danh mục."));
        return Ok(ApiResponse<DanhMucDto>.Ok(new DanhMucDto(d.MaDM, d.TenDM, d.MoTa, d.TrangThai,
            d.SanPhams.Count(s => s.TrangThai))));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] DanhMucCreateDto dto)
    {
        if (await _db.DanhMucs.AnyAsync(d => d.TenDM == dto.TenDM))
            return BadRequest(ApiResponse<object>.Fail("Tên danh mục đã tồn tại."));
        var dm = new DanhMuc { TenDM = dto.TenDM, MoTa = dto.MoTa, TrangThai = true };
        _db.DanhMucs.Add(dm);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { dm.MaDM }, "Thêm danh mục thành công."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] DanhMucCreateDto dto)
    {
        var dm = await _db.DanhMucs.FindAsync(id);
        if (dm is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        dm.TenDM = dto.TenDM; dm.MoTa = dto.MoTa;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        var dm = await _db.DanhMucs.FindAsync(id);
        if (dm is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        dm.TrangThai = !dm.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { dm.TrangThai },
            dm.TrangThai ? "Đã hiển thị danh mục." : "Đã ẩn danh mục."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var dm = await _db.DanhMucs.Include(d => d.SanPhams).FirstOrDefaultAsync(d => d.MaDM == id);
        if (dm is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        if (dm.SanPhams.Any()) return BadRequest(ApiResponse<object>.Fail("Không thể xóa danh mục đang có sản phẩm."));
        _db.DanhMucs.Remove(dm);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa danh mục."));
    }
}

using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/sanpham")]
public class SanPhamController : ControllerBase
{
    private readonly AppDbContext _db;
    public SanPhamController(AppDbContext db) => _db = db;

    // GET /api/sanpham
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] QueryParams q,
        [FromQuery] int? maDM,
        [FromQuery] decimal? giaMin,
        [FromQuery] decimal? giaMax,
        [FromQuery] bool? trangThai)
    {
        var query = _db.SanPhams
            .Include(s => s.DanhMuc)
            .Include(s => s.SoLuongSps)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(s => s.TenSP.Contains(q.Search) ||
                                     (s.MauSac != null && s.MauSac.Contains(q.Search)) ||
                                     (s.ChatLieu != null && s.ChatLieu.Contains(q.Search)));
        if (maDM.HasValue)     query = query.Where(s => s.MaDM == maDM);
        if (giaMin.HasValue)   query = query.Where(s => s.Gia >= giaMin);
        if (giaMax.HasValue)   query = query.Where(s => s.Gia <= giaMax);
        if (trangThai.HasValue)query = query.Where(s => s.TrangThai == trangThai);

        query = q.SortBy switch
        {
            "gia"         => q.SortDir == "asc" ? query.OrderBy(s => s.Gia) : query.OrderByDescending(s => s.Gia),
            "tenSP"       => q.SortDir == "asc" ? query.OrderBy(s => s.TenSP) : query.OrderByDescending(s => s.TenSP),
            "soLuongTon"  => q.SortDir == "asc" ? query.OrderBy(s => s.SoLuongTon) : query.OrderByDescending(s => s.SoLuongTon),
            _             => query.OrderByDescending(s => s.NgayTao)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(s => new SanPhamDto(
                s.MaSP, s.MaDM, s.DanhMuc.TenDM, s.TenSP, s.Gia,
                s.SoLuongTon, s.MoTa, s.HinhAnh,
                s.KichThuoc, s.MauSac, s.ChatLieu,
                s.TrangThai, s.NgayTao))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<SanPhamDto>>.Ok(new PagedResult<SanPhamDto>
        {
            Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize
        }));
    }

    // GET /api/sanpham/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _db.SanPhams
            .Include(x => x.DanhMuc)
            .Include(x => x.SoLuongSps)
            .FirstOrDefaultAsync(x => x.MaSP == id);
        if (s is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<SanPhamDto>.Ok(new SanPhamDto(
            s.MaSP, s.MaDM, s.DanhMuc.TenDM, s.TenSP, s.Gia,
            s.SoLuongTon, s.MoTa, s.HinhAnh, s.KichThuoc, s.MauSac, s.ChatLieu, s.TrangThai, s.NgayTao)));
    }

    // POST /api/sanpham
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SanPhamCreateDto dto)
    {
        var sp = new SanPham
        {
            MaDM = dto.MaDM, TenSP = dto.TenSP, Gia = dto.Gia,
            SoLuongTon = dto.SoLuongTon, MoTa = dto.MoTa, HinhAnh = dto.HinhAnh,
            KichThuoc = dto.KichThuoc, MauSac = dto.MauSac, ChatLieu = dto.ChatLieu,
            TrangThai = true, NgayTao = DateTime.Now
        };
        _db.SanPhams.Add(sp);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { sp.MaSP }, "Thêm sản phẩm thành công."));
    }

    // PUT /api/sanpham/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] SanPhamUpdateDto dto)
    {
        var sp = await _db.SanPhams.FindAsync(id);
        if (sp is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        sp.MaDM = dto.MaDM; sp.TenSP = dto.TenSP; sp.Gia = dto.Gia;
        sp.SoLuongTon = dto.SoLuongTon; sp.MoTa = dto.MoTa; sp.HinhAnh = dto.HinhAnh;
        sp.KichThuoc = dto.KichThuoc; sp.MauSac = dto.MauSac; sp.ChatLieu = dto.ChatLieu;
        sp.TrangThai = dto.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    // DELETE /api/sanpham/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var sp = await _db.SanPhams.FindAsync(id);
        if (sp is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        sp.TrangThai = false; // soft delete
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã ẩn sản phẩm."));
    }
}

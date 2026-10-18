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

    private static int GetSizeRank(string? size)
    {
        return (size ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "S" => 1,
            "M" => 2,
            "L" => 3,
            "XL" => 4,
            "XXL" => 5,
            "3XL" => 6,
            _ => 99
        };
    }

    private static SanPhamDto MapToDto(SanPham s)
    {
        var variants = s.SoLuongSps
            .OrderBy(v => GetSizeRank(v.KichThuoc))
            .ThenBy(v => v.KichThuoc)
            .ThenBy(v => v.MauSac)
            .ToList();

        decimal unifiedGia = variants.Any() ? variants.Max(v => v.GiaBan) : 0m;
        int totalStock = variants.Sum(v => v.SoLuongTon);
        string? hinhAnh = s.HinhAnh ?? variants.OrderBy(v => v.MaSL).Select(v => v.HinhAnh).FirstOrDefault();

        string? kichThuoc = variants.Any()
            ? string.Join(", ", variants.Select(v => v.KichThuoc).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct())
            : null;
        string? mauSac = variants.Any()
            ? string.Join(", ", s.SoLuongSps.OrderBy(v => v.MaSL).Select(v => v.MauSac).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct())
            : null;

        var bienThes = variants
            .Select(v => new BienTheSpDto(
                v.MaSL,
                v.SKU,
                v.KichThuoc,
                v.MauSac,
                unifiedGia,
                v.SoLuongTon
            ))
            .ToList();

        return new SanPhamDto(
            s.MaSP,
            s.MaDM,
            s.DanhMuc?.TenDM ?? string.Empty,
            s.TenSP,
            unifiedGia,
            totalStock,
            s.MoTa,
            hinhAnh,
            kichThuoc,
            mauSac,
            s.ChatLieu,
            s.TrangThai,
            s.NgayTao,
            s.HinhAnhMoTa,
            bienThes
        );
    }

    // GET /api/sanpham
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] QueryParams q,
        [FromQuery] int? maDM,
        [FromQuery] decimal? giaMin,
        [FromQuery] decimal? giaMax,
        [FromQuery] bool? trangThai)
    {
        int page = Math.Max(q.Page, 1);
        int pageSize = Math.Clamp(q.PageSize, 1, 100);
        var query = _db.SanPhams.AsNoTracking()
            .Include(s => s.DanhMuc)
            .Include(s => s.SoLuongSps)
            .Where(s => !s.DaXoa && s.DanhMuc.TrangThai);
        if (!User.IsInRole("Admin") && !User.IsInRole("QuanLy"))
            query = query.Where(s => s.TrangThai);

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(s => s.TenSP.Contains(q.Search) ||
                                     s.SoLuongSps.Any(v => v.MauSac.Contains(q.Search)) ||
                                     (s.ChatLieu != null && s.ChatLieu.Contains(q.Search)));
        if (maDM.HasValue)     query = query.Where(s => s.MaDM == maDM.Value);
        if (giaMin.HasValue)   query = query.Where(s => s.SoLuongSps.Max(v => (decimal?)v.GiaBan) >= giaMin.Value);
        if (giaMax.HasValue)   query = query.Where(s => s.SoLuongSps.Max(v => (decimal?)v.GiaBan) <= giaMax.Value);
        if (trangThai.HasValue)query = query.Where(s => s.TrangThai == trangThai);

        query = q.SortBy switch
        {
            "gia"         => q.SortDir == "asc" ? query.OrderBy(s => s.SoLuongSps.Max(v => (decimal?)v.GiaBan)) : query.OrderByDescending(s => s.SoLuongSps.Max(v => (decimal?)v.GiaBan)),
            "tenSP"       => q.SortDir == "asc" ? query.OrderBy(s => s.TenSP) : query.OrderByDescending(s => s.TenSP),
            "soLuongTon"  => q.SortDir == "asc" ? query.OrderBy(s => s.SoLuongSps.Sum(v => (int?)v.SoLuongTon)) : query.OrderByDescending(s => s.SoLuongSps.Sum(v => (int?)v.SoLuongTon)),
            _             => query.OrderByDescending(s => s.NgayTao)
        };

        var total = await query.CountAsync();
        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = rawItems.Select(MapToDto).ToList();

        return Ok(ApiResponse<PagedResult<SanPhamDto>>.Ok(new PagedResult<SanPhamDto>
        {
            Items = items, TotalItems = total, Page = page, PageSize = pageSize
        }));
    }

    // GET /api/sanpham/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var detailQuery = _db.SanPhams.AsNoTracking()
            .Include(x => x.DanhMuc)
            .Include(x => x.SoLuongSps)
            .Where(x => x.MaSP == id && !x.DaXoa && x.DanhMuc.TrangThai);
        if (!User.IsInRole("Admin") && !User.IsInRole("QuanLy"))
            detailQuery = detailQuery.Where(x => x.TrangThai);
        var s = await detailQuery.FirstOrDefaultAsync();
        if (s is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<SanPhamDto>.Ok(MapToDto(s)));
    }

    // POST /api/sanpham
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SanPhamCreateDto dto)
    {
        if (!await _db.DanhMucs.AnyAsync(category => category.MaDM == dto.MaDM && category.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Danh mục không tồn tại hoặc đang bị khóa."));

        var sp = new SanPham
        {
            MaDM = dto.MaDM, TenSP = dto.TenSP, Gia = dto.Gia,
            SoLuongTon = dto.SoLuongTon, MoTa = dto.MoTa, HinhAnh = dto.HinhAnh,
            HinhAnhMoTa = dto.HinhAnhMoTa,
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
        var sp = await _db.SanPhams.FirstOrDefaultAsync(product => product.MaSP == id && !product.DaXoa);
        if (sp is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        if (!await _db.DanhMucs.AnyAsync(category => category.MaDM == dto.MaDM && category.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Danh mục không tồn tại hoặc đang bị khóa."));
        sp.MaDM = dto.MaDM; sp.TenSP = dto.TenSP; sp.Gia = dto.Gia;
        sp.SoLuongTon = dto.SoLuongTon; sp.MoTa = dto.MoTa; sp.HinhAnh = dto.HinhAnh;
        if (dto.HinhAnhMoTa is not null) sp.HinhAnhMoTa = dto.HinhAnhMoTa;
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
        sp.TrangThai = false;
        sp.DaXoa = true;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã ẩn sản phẩm."));
    }
}

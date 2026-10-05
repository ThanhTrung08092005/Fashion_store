using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/phanhoi")]
[Authorize]
public class PhanHoiController : ControllerBase
{
    private readonly AppDbContext _db;
    public PhanHoiController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q, [FromQuery] string? trangThai, [FromQuery] string? loai)
    {
        var query = _db.PhanHois
            .Include(p => p.KhachHang)
            .Include(p => p.SanPham)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(p => p.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(loai))      query = query.Where(p => p.LoaiPhanHoi == loai);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => (p.TieuDe != null && p.TieuDe.Contains(q.Search)) ||
                                      p.KhachHang.HoTen.Contains(q.Search));

        query = q.SortDir == "asc"
            ? query.OrderBy(p => p.NgayGui)
            : query.OrderByDescending(p => p.NgayGui);

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<PhanHoiDto>>.Ok(
            new PagedResult<PhanHoiDto> { Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize }));
    }

    // GET /api/phanhoi/my  — khách hàng xem của mình
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh   = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return NotFound();

        var items = await _db.PhanHois
            .Include(p => p.SanPham)
            .Where(p => p.MaKH == kh.MaKH)
            .OrderByDescending(p => p.NgayGui)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PhanHoiDto>>.Ok(items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.PhanHois
            .Include(x => x.KhachHang).Include(x => x.SanPham)
            .FirstOrDefaultAsync(x => x.MaPH == id);
        if (p is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        return Ok(ApiResponse<PhanHoiDto>.Ok(ToDto(p)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PhanHoiCreateDto dto)
    {
        if (!User.IsInRole("KhachHang"))
            return Forbid();

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc khách hàng nào."));

        var ph = new PhanHoi
        {
            MaKH = kh.MaKH, MaSP = dto.MaSP, TieuDe = dto.TieuDe,
            NoiDung = dto.NoiDung, LoaiPhanHoi = dto.LoaiPhanHoi,
            TrangThai = "Chưa xử lý", NgayGui = DateTime.Now
        };
        _db.PhanHois.Add(ph);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { ph.MaPH }, "Gửi phản hồi thành công."));
    }

    // PATCH /api/phanhoi/{id}/reply  — quản lý phản hồi
    [HttpPatch("{id:int}/reply")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> Reply(int id, [FromBody] PhanHoiReplyDto dto)
    {
        var ph = await _db.PhanHois.FindAsync(id);
        if (ph is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var ql = await _db.QuanLys.FirstOrDefaultAsync(q => q.MaTK == maTK);
        if (ql is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc quản lý nào."));

        ph.MaQL           = ql.MaQL;
        ph.PhanHoiQuanLy  = dto.PhanHoiQuanLy;
        ph.TrangThai      = dto.TrangThai;
        ph.NgayXuLy       = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã cập nhật phản hồi."));
    }

    private static PhanHoiDto ToDto(PhanHoi p) => new(
        p.MaPH, p.MaKH, p.KhachHang?.HoTen ?? "",
        p.MaSP, p.SanPham?.TenSP,
        p.TieuDe, p.NoiDung, p.LoaiPhanHoi,
        p.TrangThai, p.PhanHoiQuanLy, p.NgayGui, p.NgayXuLy);
}

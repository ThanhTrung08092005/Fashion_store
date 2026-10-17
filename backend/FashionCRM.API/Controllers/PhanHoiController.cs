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
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q, [FromQuery] string? trangThai, [FromQuery] string? loai, [FromQuery] int? soSao)
    {
        var query = _db.PhanHois
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(p => p.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(loai))      query = query.Where(p => p.LoaiPhanHoi == loai);
        if (soSao.HasValue) query = query.Where(p => p.SoSao == soSao.Value);

        var ordered = q.SortDir == "asc"
            ? query.OrderBy(p => p.NgayGui)
            : query.OrderByDescending(p => p.NgayGui);
        var rows = await ordered.Select(p => new PhanHoiDto(
            p.MaPH, p.MaKH, p.KhachHang.HoTen,
            p.MaSP, p.SanPham != null ? p.SanPham.TenSP : null,
            p.TieuDe, p.NoiDung, p.LoaiPhanHoi, p.SoSao,
            p.TrangThai, p.PhanHoiQuanLy, p.NgayGui, p.NgayXuLy))
            .ToListAsync();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            string term = NormalizeSearchText(q.Search);
            if (term.Length > 0)
                rows = rows.Where(item => NormalizeSearchText($"{item.HoTenKH} {item.TenSP} {item.TieuDe} {item.NoiDung} {item.PhanHoiQuanLy}").Contains(term)).ToList();
        }

        int page = Math.Max(q.Page, 1);
        int pageSize = Math.Clamp(q.PageSize, 1, 100);
        var total = rows.Count;
        var items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(ApiResponse<PagedResult<PhanHoiDto>>.Ok(
            new PagedResult<PhanHoiDto> { Items = items, TotalItems = total, Page = page, PageSize = pageSize }));
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

        if (!int.TryParse(User.FindFirst("maTK")?.Value, out var maTK)) return Unauthorized();
        var kh = await _db.KhachHangs.Include(customer => customer.TaiKhoan)
            .FirstOrDefaultAsync(customer => customer.MaTK == maTK && !customer.DaXoa);
        if (kh is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc khách hàng nào."));
        if (!kh.TaiKhoan.TrangThai) return Unauthorized(ApiResponse<object>.Fail("Tài khoản khách hàng không hoạt động."));

        if (dto.SoSao.HasValue && dto.SoSao.Value is < 1 or > 5)
            return BadRequest(ApiResponse<object>.Fail("Số sao phải nằm trong khoảng từ 1 đến 5."));

        var ph = new PhanHoi
        {
            MaKH = kh.MaKH, MaSP = dto.MaSP, TieuDe = dto.TieuDe,
            NoiDung = dto.NoiDung, LoaiPhanHoi = dto.LoaiPhanHoi, SoSao = dto.SoSao,
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
        if (string.IsNullOrWhiteSpace(dto.PhanHoiQuanLy))
            return BadRequest(ApiResponse<object>.Fail("Nội dung phúc đáp không được để trống."));
        if (!string.IsNullOrWhiteSpace(ph.PhanHoiQuanLy) || ph.NgayXuLy.HasValue || ph.TrangThai == "Đã xử lý")
            return Conflict(ApiResponse<object>.Fail("Phản hồi này đã được phúc đáp."));

        if (!int.TryParse(User.FindFirst("maTK")?.Value, out var maTK)) return Unauthorized();
        var ql = await _db.QuanLys.FirstOrDefaultAsync(q => q.MaTK == maTK);
        if (ql is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc quản lý nào."));

        var reply = dto.PhanHoiQuanLy.Trim();
        var now = DateTime.Now;
        int updated = await _db.PhanHois
            .Where(item => item.MaPH == id && (item.PhanHoiQuanLy == null || item.PhanHoiQuanLy == "")
                && item.NgayXuLy == null && item.TrangThai != "Đã xử lý")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.MaQL, (int?)ql.MaQL)
                .SetProperty(item => item.PhanHoiQuanLy, reply)
                .SetProperty(item => item.TrangThai, "Đã xử lý")
                .SetProperty(item => item.NgayXuLy, (DateTime?)now));
        if (updated == 0) return Conflict(ApiResponse<object>.Fail("Phản hồi này đã được phúc đáp."));
        return Ok(ApiResponse<object>.Ok(new { NgayXuLy = now }, "Đã cập nhật phản hồi."));
    }

    private static PhanHoiDto ToDto(PhanHoi p) => new(
        p.MaPH, p.MaKH, p.KhachHang?.HoTen ?? "",
        p.MaSP, p.SanPham?.TenSP,
        p.TieuDe, p.NoiDung, p.LoaiPhanHoi,
        p.SoSao, p.TrangThai, p.PhanHoiQuanLy, p.NgayGui, p.NgayXuLy);

    private static string NormalizeSearchText(string value)
    {
        var decomposed = value.Replace('đ', 'd').Replace('Đ', 'D').Normalize(System.Text.NormalizationForm.FormD);
        return new string(decomposed
            .Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}

using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/khaosat")]
[Authorize]
public class KhaoSatController : ControllerBase
{
    private readonly AppDbContext _db;
    public KhaoSatController(AppDbContext db) => _db = db;

    // GET /api/khaosat  — quản lý xem tất cả
    [HttpGet]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q)
    {
        var query = _db.KhaoSats.Include(k => k.CauHois).Include(k => k.PhieuKhaoSats).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(k => k.TenKhaoSat.Contains(q.Search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(k => k.NgayBatDau)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(k => new KhaoSatDto(
                k.MaKS, k.MaQL, k.TenKhaoSat, k.MoTa,
                k.NgayBatDau, k.NgayKetThuc, k.TrangThai,
                k.CauHois.Count,
                k.PhieuKhaoSats.Count(p => p.TrangThai == "Đã hoàn thành")))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<KhaoSatDto>>.Ok(
            new PagedResult<KhaoSatDto> { Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize }));
    }

    // GET /api/khaosat/my  — khách hàng xem khảo sát được giao
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh   = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return NotFound();

        var assigned = await _db.PhieuKhaoSats
            .Include(p => p.KhaoSat).ThenInclude(k => k.CauHois)
            .Where(p => p.MaKH == kh.MaKH)
            .Select(p => new
            {
                p.MaPhieuKS, p.TrangThai,
                KhaoSat = new KhaoSatDto(
                    p.KhaoSat.MaKS, p.KhaoSat.MaQL,
                    p.KhaoSat.TenKhaoSat, p.KhaoSat.MoTa,
                    p.KhaoSat.NgayBatDau, p.KhaoSat.NgayKetThuc,
                    p.KhaoSat.TrangThai,
                    p.KhaoSat.CauHois.Count,
                    p.KhaoSat.PhieuKhaoSats.Count(x => x.TrangThai == "Đã hoàn thành")),
                CauHois = p.KhaoSat.CauHois.OrderBy(c => c.ThuTu).Select(c => new
                {
                    c.MaCauHoi, c.NoiDung, c.LoaiCauHoi, c.BatBuoc,
                    LuaChons = c.LuaChons.OrderBy(l => l.ThuTu).Select(l => new { l.MaLuaChon, l.NoiDung })
                })
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(assigned));
    }

    // GET /api/khaosat/{id}/detail  — chi tiết + câu hỏi
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var ks = await _db.KhaoSats
            .Include(k => k.CauHois).ThenInclude(c => c.LuaChons)
            .FirstOrDefaultAsync(k => k.MaKS == id);
        if (ks is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khảo sát."));
        return Ok(ApiResponse<object>.Ok(new
        {
            ks.MaKS, ks.TenKhaoSat, ks.MoTa, ks.NgayBatDau, ks.NgayKetThuc, ks.TrangThai,
            CauHois = ks.CauHois.OrderBy(c => c.ThuTu).Select(c => new
            {
                c.MaCauHoi, c.NoiDung, c.LoaiCauHoi, c.ThuTu, c.BatBuoc,
                LuaChons = c.LuaChons.OrderBy(l => l.ThuTu).Select(l => new { l.MaLuaChon, l.NoiDung })
            })
        }));
    }

    // POST /api/khaosat  — quản lý tạo khảo sát
    [HttpPost]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> Create([FromBody] KhaoSatCreateDto dto)
    {
        if (dto.NgayKetThuc < dto.NgayBatDau)
            return BadRequest(ApiResponse<object>.Fail("Ngày kết thúc phải sau ngày bắt đầu."));

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var ql = await _db.QuanLys.FirstOrDefaultAsync(q => q.MaTK == maTK);
        if (ql is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc quản lý nào."));

        var ks = new KhaoSat
        {
            MaQL = ql.MaQL, TenKhaoSat = dto.TenKhaoSat,
            MoTa = dto.MoTa, NgayBatDau = dto.NgayBatDau,
            NgayKetThuc = dto.NgayKetThuc, TrangThai = true
        };
        _db.KhaoSats.Add(ks);
        await _db.SaveChangesAsync();

        foreach (var cd in dto.CauHois)
        {
            var ch = new CauHoiKhaoSat
            {
                MaKS = ks.MaKS, NoiDung = cd.NoiDung,
                LoaiCauHoi = cd.LoaiCauHoi, ThuTu = cd.ThuTu, BatBuoc = cd.BatBuoc
            };
            _db.CauHoiKhaoSats.Add(ch);
            await _db.SaveChangesAsync();

            if (cd.LuaChons is not null)
            {
                int order = 1;
                foreach (var lc in cd.LuaChons)
                    _db.LuaChonCauHois.Add(new LuaChonCauHoi { MaCauHoi = ch.MaCauHoi, NoiDung = lc, ThuTu = order++ });
                await _db.SaveChangesAsync();
            }
        }

        return Ok(ApiResponse<object>.Ok(new { ks.MaKS }, "Tạo khảo sát thành công."));
    }

    // POST /api/khaosat/{id}/send  — gửi phiếu đến danh sách KH
    [HttpPost("{id:int}/send")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> SendToCustomers(int id, [FromBody] List<int> maKHList)
    {
        var ks = await _db.KhaoSats.FindAsync(id);
        if (ks is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));

        int sent = 0;
        foreach (var maKH in maKHList)
        {
            if (await _db.PhieuKhaoSats.AnyAsync(p => p.MaKS == id && p.MaKH == maKH)) continue;
            _db.PhieuKhaoSats.Add(new PhieuKhaoSat { MaKS = id, MaKH = maKH, TrangThai = "Đang làm" });
            sent++;
        }
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Sent = sent }, $"Đã gửi đến {sent} khách hàng."));
    }

    // POST /api/khaosat/submit  — khách hàng nộp bài
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitSurveyDto dto)
    {
        if (!User.IsInRole("KhachHang"))
            return Forbid();

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc khách hàng nào."));

        var phieu = await _db.PhieuKhaoSats.FirstOrDefaultAsync(p => p.MaPhieuKS == dto.MaPhieuKS && p.MaKH == kh.MaKH);
        if (phieu is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phiếu khảo sát của bạn."));
        if (phieu.TrangThai == "Đã hoàn thành")
            return BadRequest(ApiResponse<object>.Fail("Phiếu đã được nộp trước đó."));

        foreach (var tl in dto.TraLois)
        {
            _db.TraLoiKhaoSats.Add(new TraLoiKhaoSat
            {
                MaPhieuKS = dto.MaPhieuKS, MaCauHoi = tl.MaCauHoi,
                MaLuaChon = tl.MaLuaChon, NoiDungTraLoi = tl.NoiDungTraLoi, SoSao = tl.SoSao
            });
        }
        phieu.TrangThai = "Đã hoàn thành";
        phieu.NgayNop   = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Nộp khảo sát thành công!"));
    }
}

// Submit DTO
public record SubmitSurveyDto(int MaPhieuKS, List<TraLoiDto> TraLois);
public record TraLoiDto(int MaCauHoi, int? MaLuaChon, string? NoiDungTraLoi, int? SoSao);

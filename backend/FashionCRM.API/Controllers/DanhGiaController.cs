using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/danhgia")]
public class DanhGiaController : ControllerBase
{
    private readonly AppDbContext _db;
    public DanhGiaController(AppDbContext db) => _db = db;

    // GET /api/danhgia?maSP=1
    [HttpGet]
    public async Task<IActionResult> GetBySanPham([FromQuery] int maSP, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.DanhGias.Include(d => d.KhachHang).Where(d => d.MaSP == maSP);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.NgayDanhGia)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DanhGiaDto(d.MaDG, d.MaKH, d.KhachHang.HoTen, d.MaSP, d.SoSao, d.NoiDung, d.NgayDanhGia))
            .ToListAsync();

        var avgStar = total > 0 ? await query.AverageAsync(d => (double)d.SoSao) : 0;
        return Ok(ApiResponse<object>.Ok(new { Items = items, TotalItems = total, Page = page, PageSize = pageSize, DiemTrungBinh = Math.Round(avgStar, 1) }));
    }

    // GET /api/danhgia/my
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMy()
    {
        if (!User.IsInRole("KhachHang")) return Forbid();

        var maTkRaw = User.FindFirst("maTK")?.Value;
        if (!int.TryParse(maTkRaw, out var maTK))
            return Unauthorized(ApiResponse<object>.Fail("Phiên đăng nhập không hợp lệ."));

        var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin khách hàng."));

        var items = await _db.DanhGias
            .Include(d => d.SanPham)
            .Where(d => d.MaKH == kh.MaKH)
            .OrderByDescending(d => d.NgayDanhGia)
            .Select(d => new
            {
                d.MaDG, d.MaKH, HoTenKH = kh.HoTen, d.MaSP,
                TenSP = d.SanPham.TenSP, d.SoSao, d.NoiDung, d.NgayDanhGia
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(items));
    }

    // POST /api/danhgia
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] DanhGiaCreateDto dto)
    {
        if (!User.IsInRole("KhachHang"))
            return Forbid();

        if (dto.SoSao < 1 || dto.SoSao > 5)
            return BadRequest(ApiResponse<object>.Fail("Số sao phải từ 1 đến 5."));

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc khách hàng nào."));

        if (await _db.DanhGias.AnyAsync(d => d.MaKH == kh.MaKH && d.MaSP == dto.MaSP))
            return BadRequest(ApiResponse<object>.Fail("Bạn đã đánh giá sản phẩm này rồi."));

        var dg = new DanhGia { MaKH = kh.MaKH, MaSP = dto.MaSP, SoSao = dto.SoSao, NoiDung = dto.NoiDung, NgayDanhGia = DateTime.Now };
        _db.DanhGias.Add(dg);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { dg.MaDG }, "Đánh giá thành công!"));
    }

    // DELETE /api/danhgia/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var dg = await _db.DanhGias.FindAsync(id);
        if (dg is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đánh giá."));
        _db.DanhGias.Remove(dg);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa đánh giá."));
    }
}

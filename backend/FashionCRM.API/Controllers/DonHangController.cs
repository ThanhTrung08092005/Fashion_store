using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/donhang")]
[Authorize]
public class DonHangController : ControllerBase
{
    private readonly AppDbContext _db;
    public DonHangController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q, [FromQuery] string? trangThai)
    {
        var query = _db.DonHangs.Include(d => d.KhachHang).Include(d => d.ChiTiets).ThenInclude(c => c.SanPham).AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(d => d.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(d => d.KhachHang.HoTen.Contains(q.Search) || d.MaDH.ToString().Contains(q.Search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.NgayDat)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(d => ToDto(d)).ToListAsync();

        return Ok(ApiResponse<PagedResult<DonHangDto>>.Ok(
            new PagedResult<DonHangDto> { Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize }));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh   = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return NotFound();

        var items = await _db.DonHangs
            .Include(d => d.KhachHang).Include(d => d.ChiTiets).ThenInclude(c => c.SanPham)
            .Where(d => d.MaKH == kh.MaKH)
            .OrderByDescending(d => d.NgayDat)
            .Select(d => ToDto(d)).ToListAsync();

        return Ok(ApiResponse<IEnumerable<DonHangDto>>.Ok(items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var d = await _db.DonHangs
            .Include(x => x.KhachHang).Include(x => x.ChiTiets).ThenInclude(c => c.SanPham)
            .FirstOrDefaultAsync(x => x.MaDH == id);
        if (d is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn hàng."));
        return Ok(ApiResponse<DonHangDto>.Ok(ToDto(d)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DonHangCreateDto dto)
    {
        if (!User.IsInRole("KhachHang"))
            return Forbid();

        var maTK = int.Parse(User.FindFirst("maTK")!.Value);
        var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        if (kh is null) return Unauthorized(ApiResponse<object>.Fail("Tài khoản hiện tại không thuộc khách hàng nào."));

        decimal tongTien = 0;
        var chiTiets = new List<ChiTietDonHang>();

        foreach (var ct in dto.ChiTiets)
        {
            var sp = await _db.SanPhams.FindAsync(ct.MaSP);
            if (sp is null) return BadRequest(ApiResponse<object>.Fail($"Sản phẩm #{ct.MaSP} không tồn tại."));
            if (sp.SoLuongTon < ct.SoLuong) return BadRequest(ApiResponse<object>.Fail($"Sản phẩm '{sp.TenSP}' không đủ hàng."));
            tongTien += sp.Gia * ct.SoLuong;
            chiTiets.Add(new ChiTietDonHang { MaSP = ct.MaSP, SoLuong = ct.SoLuong, DonGia = sp.Gia });
            sp.SoLuongTon -= ct.SoLuong;
        }

        var dh = new DonHang
        {
            MaKH = kh.MaKH, NgayDat = DateTime.Now, TongTien = tongTien,
            TrangThai = "Chờ xác nhận", DiaChiGiaoHang = dto.DiaChiGiaoHang,
            SoDienThoaiNhan = dto.SoDienThoaiNhan
        };
        _db.DonHangs.Add(dh);
        await _db.SaveChangesAsync();

        chiTiets.ForEach(c => c.MaDH = dh.MaDH);
        _db.ChiTietDonHangs.AddRange(chiTiets);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { dh.MaDH, TongTien = tongTien }, "Đặt hàng thành công!"));
    }

    [HttpPatch("{id:int}/trangthai")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTrangThaiDonHangDto dto)
    {
        var dh = await _db.DonHangs.FindAsync(id);
        if (dh is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        dh.TrangThai = dto.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật trạng thái thành công."));
    }

    private static DonHangDto ToDto(DonHang d) => new(
        d.MaDH, d.MaKH, d.KhachHang?.HoTen ?? "",
        d.NgayDat, d.TongTien, d.TrangThai,
        d.DiaChiGiaoHang, d.SoDienThoaiNhan,
        d.ChiTiets.Select(c => new ChiTietDonHangDto(
            c.MaSP, c.SanPham?.TenSP ?? "", c.SoLuong, c.DonGia, c.SoLuong * c.DonGia
        )).ToList());
}

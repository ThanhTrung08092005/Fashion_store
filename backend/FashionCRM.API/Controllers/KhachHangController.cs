using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using FashionCRM.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Controllers;

[ApiController]
[Route("api/khachhang")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _db;
    public KhachHangController(AppDbContext db) => _db = db;

    // GET /api/khachhang?page=1&pageSize=10&search=...&gioiTinh=...
    [HttpGet]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> GetAll(
        [FromQuery] QueryParams q,
        [FromQuery] string? gioiTinh,
        [FromQuery] bool? trangThai)
    {
        var query = _db.KhachHangs
            .Include(k => k.TaiKhoan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(k =>
                k.HoTen.Contains(q.Search) ||
                k.Email.Contains(q.Search) ||
                (k.SoDienThoai != null && k.SoDienThoai.Contains(q.Search)));

        if (!string.IsNullOrWhiteSpace(gioiTinh))
            query = query.Where(k => k.GioiTinh == gioiTinh);

        if (trangThai.HasValue)
            query = query.Where(k => k.TaiKhoan.TrangThai == trangThai.Value);

        query = q.SortBy switch
        {
            "hoTen"      => q.SortDir == "asc" ? query.OrderBy(k => k.HoTen) : query.OrderByDescending(k => k.HoTen),
            "ngayDangKy" => q.SortDir == "asc" ? query.OrderBy(k => k.NgayDangKy) : query.OrderByDescending(k => k.NgayDangKy),
            _            => query.OrderByDescending(k => k.NgayDangKy)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(k => new KhachHangDto(
                k.MaKH, k.HoTen, k.Email, k.SoDienThoai,
                k.NgaySinh, k.GioiTinh, k.DiaChi, k.SoThich,
                k.NgayDangKy, k.TaiKhoan.TrangThai))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<KhachHangDto>>.Ok(new PagedResult<KhachHangDto>
        {
            Items = items, TotalItems = total, Page = q.Page, PageSize = q.PageSize
        }));
    }

    // GET /api/khachhang/me
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentCustomer()
    {
        var maTkRaw = User.FindFirst("maTK")?.Value;
        if (string.IsNullOrWhiteSpace(maTkRaw) || !int.TryParse(maTkRaw, out var maTk))
            return Unauthorized(ApiResponse<object>.Fail("Phiên đăng nhập không hợp lệ."));

        var k = await _db.KhachHangs
            .Include(k => k.TaiKhoan)
            .FirstOrDefaultAsync(k => k.MaTK == maTk);
        if (k is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin khách hàng."));

        return Ok(ApiResponse<KhachHangDto>.Ok(new KhachHangDto(
            k.MaKH, k.HoTen, k.Email, k.SoDienThoai,
            k.NgaySinh, k.GioiTinh, k.DiaChi, k.SoThich,
            k.NgayDangKy, k.TaiKhoan.TrangThai)));
    }

    // GET /api/khachhang/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var k = await _db.KhachHangs
            .Include(k => k.TaiKhoan)
            .FirstOrDefaultAsync(k => k.MaKH == id);
        if (k is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khách hàng."));

        return Ok(ApiResponse<KhachHangDto>.Ok(new KhachHangDto(
            k.MaKH, k.HoTen, k.Email, k.SoDienThoai,
            k.NgaySinh, k.GioiTinh, k.DiaChi, k.SoThich,
            k.NgayDangKy, k.TaiKhoan.TrangThai)));
    }

    // POST /api/khachhang  (Quản lý thêm thủ công)
    [HttpPost]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> Create([FromBody] KhachHangCreateDto dto)
    {
        if (await _db.KhachHangs.AnyAsync(k => k.Email == dto.Email))
            return BadRequest(ApiResponse<object>.Fail("Email đã tồn tại."));

        var kh = new KhachHang
        {
            MaTK = dto.MaTK, HoTen = dto.HoTen, Email = dto.Email,
            SoDienThoai = dto.SoDienThoai, NgaySinh = dto.NgaySinh,
            GioiTinh = dto.GioiTinh, DiaChi = dto.DiaChi, SoThich = dto.SoThich,
            NgayDangKy = DateTime.Now
        };
        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { kh.MaKH }, "Thêm khách hàng thành công."));
    }

    // POST /api/khachhang/create-with-account  (Quản lý thêm khách hàng mới)
    [HttpPost("create-with-account")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> CreateWithAccount([FromBody] KhachHangCreateWithAccountDto dto)
    {
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
            return BadRequest(ApiResponse<object>.Fail("Tên đăng nhập đã tồn tại."));
        if (await _db.KhachHangs.AnyAsync(k => k.Email == dto.Email))
            return BadRequest(ApiResponse<object>.Fail("Email đã tồn tại."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var tk = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaVaiTro = 3,
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            var kh = new KhachHang
            {
                MaTK = tk.MaTK, HoTen = dto.HoTen, Email = dto.Email,
                SoDienThoai = dto.SoDienThoai, NgaySinh = dto.NgaySinh,
                GioiTinh = dto.GioiTinh, DiaChi = dto.DiaChi, SoThich = dto.SoThich,
                NgayDangKy = DateTime.Now
            };
            _db.KhachHangs.Add(kh);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.Ok(new { kh.MaKH, tk.MaTK }, "Thêm khách hàng thành công."));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT /api/khachhang/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentCustomer([FromBody] KhachHangUpdateDto dto)
    {
        var maTkRaw = User.FindFirst("maTK")?.Value;
        if (string.IsNullOrWhiteSpace(maTkRaw) || !int.TryParse(maTkRaw, out var maTk))
            return Unauthorized(ApiResponse<object>.Fail("Phiên đăng nhập không hợp lệ."));

        var kh = await _db.KhachHangs
            .Include(k => k.TaiKhoan)
            .FirstOrDefaultAsync(k => k.MaTK == maTk);
        if (kh is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin khách hàng."));

        if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(kh.Email, dto.Email, StringComparison.OrdinalIgnoreCase)
            && await _db.KhachHangs.AnyAsync(k => k.Email == dto.Email && k.MaKH != kh.MaKH))
        {
            return BadRequest(ApiResponse<object>.Fail("Email này đã được sử dụng bởi tài khoản khác."));
        }

        kh.HoTen = dto.HoTen?.Trim() ?? kh.HoTen;
        kh.Email = dto.Email?.Trim() ?? kh.Email;
        kh.SoDienThoai = dto.SoDienThoai;
        kh.NgaySinh = dto.NgaySinh;
        kh.GioiTinh = dto.GioiTinh;
        kh.DiaChi = dto.DiaChi;
        kh.SoThich = dto.SoThich;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new
        {
            kh.MaKH,
            kh.HoTen,
            kh.Email,
            kh.SoDienThoai,
            kh.NgaySinh,
            kh.GioiTinh,
            kh.DiaChi,
            kh.SoThich
        }, "Cập nhật thông tin cá nhân thành công."));
    }

    // PUT /api/khachhang/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] KhachHangUpdateDto dto)
    {
        var kh = await _db.KhachHangs.FindAsync(id);
        if (kh is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));

        kh.HoTen = dto.HoTen; kh.Email = dto.Email;
        kh.SoDienThoai = dto.SoDienThoai; kh.NgaySinh = dto.NgaySinh;
        kh.GioiTinh = dto.GioiTinh; kh.DiaChi = dto.DiaChi;
        kh.SoThich  = dto.SoThich;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    // DELETE /api/khachhang/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> Delete(int id)
    {
        var kh = await _db.KhachHangs.FindAsync(id);
        if (kh is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        _db.KhachHangs.Remove(kh);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa khách hàng."));
    }

    // PATCH /api/khachhang/{id}/lock
    [HttpPatch("{id:int}/lock")]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var tk = await _db.TaiKhoans
            .Include(t => t.KhachHang)
            .FirstOrDefaultAsync(t => t.KhachHang != null && t.KhachHang.MaKH == id);
        if (tk is null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy."));
        tk.TrangThai = !tk.TrangThai;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { tk.TrangThai }, tk.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản."));
    }
}

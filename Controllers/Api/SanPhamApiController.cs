using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public SanPhamApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/SanPhamApi?search=ao&maDM=1 - Lọc và tìm kiếm sản phẩm
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? maDM)
        {
            var query = _context.SanPham.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.TenSP.Contains(search) || (s.ChatLieu != null && s.ChatLieu.Contains(search)));

            if (maDM.HasValue)
                query = query.Where(s => s.MaDM == maDM.Value);

            var list = await query.Select(s => new
            {
                s.MaSP,
                s.TenSP,
                s.MaDM,
                TenDanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDM : null,
                s.ChatLieu,
                s.MoTa,
                s.TrangThai,
                s.NgayTao,
                TongTonKho = s.SoLuongSps.Sum(sl => sl.SoLuongTon)
            }).ToListAsync();

            return Ok(list);
        }

        // GET: api/SanPhamApi/5 - Chi tiết sản phẩm kèm size/màu
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.SanPham
                .AsNoTracking()
                .Include(s => s.DanhMuc)
                .Include(s => s.SoLuongSps)
                .FirstOrDefaultAsync(s => s.MaSP == id);

            if (item == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            return Ok(new
            {
                item.MaSP,
                item.TenSP,
                item.MaDM,
                TenDanhMuc = item.DanhMuc?.TenDM,
                item.MoTa,
                item.ChatLieu,
                item.TrangThai,
                item.NgayTao,
                ChiTietTonKho = item.SoLuongSps.Select(sl => new
                {
                    sl.MaSL,
                    sl.SKU,
                    sl.KichThuoc,
                    sl.MauSac,
                    sl.GiaBan,
                    sl.SoLuongTon
                })
            });
        }
    }
}

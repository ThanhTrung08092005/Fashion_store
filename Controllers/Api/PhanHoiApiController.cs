using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PhanHoiApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public PhanHoiApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/PhanHoiApi - Quản lý xem danh sách phản hồi (có lọc theo số sao hoặc trạng thái)
        [HttpGet]
        [Authorize(Roles = "Admin, Quản lý")]
        public async Task<IActionResult> GetAll([FromQuery] int? soSao, [FromQuery] string? trangThai)
        {
            var query = _context.PhanHoi.AsNoTracking().AsQueryable();

            if (soSao.HasValue)
                query = query.Where(p => p.SoSao == soSao.Value);

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(p => p.TrangThai == trangThai);

            var list = await query
                .OrderByDescending(p => p.NgayGui)
                .Select(p => new
                {
                    p.MaPH,
                    TenKhachHang = p.KhachHang != null ? p.KhachHang.HoTen : "Ẩn danh",
                    TenSanPham = p.SanPham != null ? p.SanPham.TenSP : "Chung",
                    p.TieuDe,
                    p.NoiDung,
                    p.SoSao,
                    p.TrangThai,
                    p.PhanHoiQuanLy,
                    p.NgayGui,
                    p.NgayXuLy
                }).ToListAsync();

            return Ok(list);
        }

        // 2. POST: api/PhanHoiApi - Khách hàng gửi đánh giá / phản hồi
        [HttpPost]
        [Authorize(Roles = "Khách hàng")]
        public async Task<IActionResult> GuiPhanHoi([FromBody] GuiPhanHoiDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NoiDung))
                return BadRequest(new { message = "Nội dung phản hồi không được để trống" });

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)) return Unauthorized();
            var customer = await _context.KhachHang.Include(kh => kh.TaiKhoan)
                .FirstOrDefaultAsync(kh => kh.MaTK == maTK && !kh.DaXoa);
            if (customer == null || customer.TaiKhoan == null || !customer.TaiKhoan.TrangThai) return Unauthorized();

            if (model.SoSao.HasValue && model.SoSao.Value is < 1 or > 5)
                return BadRequest(new { message = "Số sao phải nằm trong khoảng từ 1 đến 5." });
            if (model.MaSP.HasValue && !await _context.SanPham.AnyAsync(product =>
                    product.MaSP == model.MaSP.Value && product.TrangThai && !product.DaXoa))
                return BadRequest(new { message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh." });

            var phanHoi = new PhanHoi
            {
                MaKH = customer.MaKH,
                MaSP = model.MaSP,
                TieuDe = model.TieuDe,
                NoiDung = model.NoiDung,
                SoSao = model.SoSao,
                LoaiPhanHoi = model.LoaiPhanHoi ?? "Đánh giá sản phẩm",
                TrangThai = "Chưa xử lý",
                NgayGui = DateTime.Now
            };

            _context.PhanHoi.Add(phanHoi);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi phản hồi thành công", maPhanHoi = phanHoi.MaPH });
        }

        // 3. PUT: api/PhanHoiApi/5/tra-loi - Quản lý phản hồi lại ý kiến khách hàng
        [HttpPut("{id}/tra-loi")]
        [Authorize(Roles = "Admin, Quản lý")]
        public async Task<IActionResult> TraLoiPhanHoi(int id, [FromBody] TraLoiPhanHoiDto model)
        {
            var phanHoi = await _context.PhanHoi.FindAsync(id);
            if (phanHoi == null) return NotFound(new { message = "Không tìm thấy phản hồi" });
            if (string.IsNullOrWhiteSpace(model.NoiDungTraLoi))
                return BadRequest(new { message = "Nội dung phúc đáp không được để trống." });
            if (model.NoiDungTraLoi.Trim().Length > 2000)
                return BadRequest(new { message = "Nội dung phúc đáp không được vượt quá 2.000 ký tự." });
            if (!string.IsNullOrWhiteSpace(phanHoi.PhanHoiQuanLy) || phanHoi.NgayXuLy.HasValue || phanHoi.TrangThai == "Đã xử lý")
                return Conflict(new { message = "Phản hồi này đã được phúc đáp." });

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)) return Unauthorized();
            var manager = await _context.QuanLy.FirstOrDefaultAsync(item => item.MaTK == maTK);
            if (manager == null) return Forbid();

            var reply = model.NoiDungTraLoi.Trim();
            var now = DateTime.Now;
            var updated = await _context.PhanHoi
                .Where(item => item.MaPH == id
                    && (item.PhanHoiQuanLy == null || item.PhanHoiQuanLy == "")
                    && item.NgayXuLy == null
                    && item.TrangThai != "Đã xử lý")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.MaQL, (int?)manager.MaQL)
                    .SetProperty(item => item.PhanHoiQuanLy, reply)
                    .SetProperty(item => item.TrangThai, "Đã xử lý")
                    .SetProperty(item => item.NgayXuLy, (DateTime?)now));
            if (updated == 0)
                return Conflict(new { message = "Phản hồi này vừa được phúc đáp bởi một quản lý khác." });
            return Ok(new { message = "Đã cập nhật câu trả lời cho khách hàng" });
        }

        // 4. DELETE: api/PhanHoiApi/5 - Xóa phản hồi (Đặt ở đây mới chuẩn trong class)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var phanHoi = await _context.PhanHoi.FindAsync(id);
            if (phanHoi == null) return NotFound(new { message = "Không tìm thấy phản hồi để xóa" });

            _context.PhanHoi.Remove(phanHoi);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa phản hồi thành công" });
        }

    }

    public class GuiPhanHoiDto
    {
        public int MaKH { get; set; }
        public int? MaSP { get; set; }
        public string? TieuDe { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public int? SoSao { get; set; }
        public string? LoaiPhanHoi { get; set; }
    }

    public class TraLoiPhanHoiDto
    {
        public string NoiDungTraLoi { get; set; } = string.Empty;
    }
}

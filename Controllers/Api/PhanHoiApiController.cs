using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhanHoiApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public PhanHoiApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/PhanHoiApi - Quản lý xem danh sách phản hồi (có lọc theo số sao hoặc trạng thái)
        [HttpGet]
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
                    p.NgayGui
                }).ToListAsync();

            return Ok(list);
        }

        // 2. POST: api/PhanHoiApi - Khách hàng gửi đánh giá / phản hồi
        [HttpPost]
        public async Task<IActionResult> GuiPhanHoi([FromBody] GuiPhanHoiDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NoiDung))
                return BadRequest(new { message = "Nội dung phản hồi không được để trống" });

            var phanHoi = new PhanHoi
            {
                MaKH = model.MaKH,
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
        public async Task<IActionResult> TraLoiPhanHoi(int id, [FromBody] TraLoiPhanHoiDto model)
        {
            var phanHoi = await _context.PhanHoi.FindAsync(id);
            if (phanHoi == null) return NotFound(new { message = "Không tìm thấy phản hồi" });

            phanHoi.PhanHoiQuanLy = model.NoiDungTraLoi;
            phanHoi.TrangThai = "Đã xử lý";
            phanHoi.NgayXuLy = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật câu trả lời cho khách hàng" });
        }

        // 4. DELETE: api/PhanHoiApi/5 - Xóa phản hồi (Đặt ở đây mới chuẩn trong class)
        [HttpDelete("{id}")]
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

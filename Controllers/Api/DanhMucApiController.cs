using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DanhMucApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public DanhMucApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/DanhMucApi - Lấy danh sách danh mục
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.DanhMuc
                .AsNoTracking()
                .Select(d => new
                {
                    d.MaDM,
                    d.TenDM,
                    d.MoTa,
                    d.TrangThai,
                    SoLuongSanPham = d.SanPhams.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        // 2. GET: api/DanhMucApi/5 - Lấy chi tiết danh mục
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.DanhMuc
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.MaDM == id);

            if (item == null) return NotFound(new { message = "Không tìm thấy danh mục" });
            return Ok(item);
        }

        // 3. POST: api/DanhMucApi - Thêm danh mục mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DanhMuc model)
        {
            if (string.IsNullOrWhiteSpace(model.TenDM))
                return BadRequest(new { message = "Tên danh mục không được để trống" });

            _context.DanhMuc.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.MaDM }, model);
        }

        // 4. PUT: api/DanhMucApi/5 - Cập nhật danh mục
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DanhMuc model)
        {
            var item = await _context.DanhMuc.FindAsync(id);
            if (item == null) return NotFound(new { message = "Không tìm thấy danh mục" });

            item.TenDM = model.TenDM;
            item.MoTa = model.MoTa;
            item.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công", data = item });
        }

        // 5. DELETE: api/DanhMucApi/5 - Xóa danh mục
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.DanhMuc.Include(d => d.SanPhams).FirstOrDefaultAsync(d => d.MaDM == id);
            if (item == null) return NotFound(new { message = "Không tìm thấy danh mục" });

            if (item.SanPhams.Any())
                return BadRequest(new { message = "Không thể xóa vì danh mục đang chứa sản phẩm" });

            _context.DanhMuc.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa danh mục thành công" });
        }
    }
}

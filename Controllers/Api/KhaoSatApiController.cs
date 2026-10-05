using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhaoSatApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public KhaoSatApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/KhaoSatApi - Danh sách các chiến dịch khảo sát
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.KhaoSat
                .AsNoTracking()
                .Select(k => new
                {
                    k.MaKS,
                    k.TenKhaoSat,
                    k.MoTa,
                    k.NgayBatDau,
                    k.NgayKetThuc,
                    k.TrangThai,
                    SoCauHoi = k.CauHoiKhaoSats.Count,
                    SoPhieuDaNop = k.PhieuKhaoSats.Count(p => p.TrangThai == "Hoàn thành")
                })
                .ToListAsync();

            return Ok(list);
        }

        // 2. GET: api/KhaoSatApi/5/cau-hoi - Lấy toàn bộ câu hỏi và đáp án lựa chọn
        [HttpGet("{id}/cau-hoi")]
        public async Task<IActionResult> GetCauHoi(int id)
        {
            var khaoSat = await _context.KhaoSat
                .AsNoTracking()
                .Include(k => k.CauHoiKhaoSats)
                    .ThenInclude(c => c.LuaChonCauHois)
                .FirstOrDefaultAsync(k => k.MaKS == id);

            if (khaoSat == null) return NotFound(new { message = "Không tìm thấy khảo sát" });

            return Ok(new
            {
                khaoSat.MaKS,
                khaoSat.TenKhaoSat,
                khaoSat.MoTa,
                CauHoi = khaoSat.CauHoiKhaoSats.OrderBy(c => c.ThuTu).Select(c => new
                {
                    c.MaCauHoi,
                    c.NoiDung,
                    c.LoaiCauHoi,
                    c.BatBuoc,
                    LuaChon = c.LuaChonCauHois.OrderBy(l => l.ThuTu).Select(l => new
                    {
                        l.MaLuaChon,
                        l.NoiDung
                    })
                })
            });
        }

        // 3. POST: api/KhaoSatApi/nop-bai - Khách hàng nộp kết quả làm khảo sát
        [HttpPost("nop-bai")]
        public async Task<IActionResult> NopBai([FromBody] NopKhaoSatDto model)
        {
            if (model.MaKS <= 0 || model.MaKH <= 0)
                return BadRequest(new { message = "Mã khảo sát và Mã khách hàng không hợp lệ" });

            var phieu = new PhieuKhaoSat
            {
                MaKS = model.MaKS,
                MaKH = model.MaKH,
                NgayBatDau = DateTime.Now,
                NgayNop = DateTime.Now,
                TrangThai = "Hoàn thành"
            };

            foreach (var ans in model.CauTraLoi)
            {
                phieu.TraLoiKhaoSats.Add(new TraLoiKhaoSat
                {
                    MaCauHoi = ans.MaCauHoi,
                    MaLuaChon = ans.MaLuaChon,
                    NoiDungTraLoi = ans.NoiDungTraLoi,
                    SoSao = ans.SoSao
                });
            }

            _context.PhieuKhaoSat.Add(phieu);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nộp bài khảo sát thành công", maPhieuKS = phieu.MaPhieuKS });
        }

        // 4. GET: api/KhaoSatApi/5/thong-ke - Báo cáo thống kê kết quả khảo sát
        [HttpGet("{id}/thong-ke")]
        public async Task<IActionResult> ThongKe(int id)
        {
            var khaoSat = await _context.KhaoSat
                .AsNoTracking()
                .Include(k => k.CauHoiKhaoSats)
                    .ThenInclude(c => c.LuaChonCauHois)
                .FirstOrDefaultAsync(k => k.MaKS == id);

            if (khaoSat == null) return NotFound(new { message = "Không tìm thấy khảo sát" });

            int tongSoPhieu = await _context.PhieuKhaoSat.CountAsync(p => p.MaKS == id && p.TrangThai == "Hoàn thành");

            var thongKeCauHoi = await _context.CauHoiKhaoSat
                .Where(c => c.MaKS == id)
                .Select(c => new
                {
                    c.MaCauHoi,
                    c.NoiDung,
                    c.LoaiCauHoi,
                    CacLuaChon = c.LuaChonCauHois.Select(l => new
                    {
                        l.MaLuaChon,
                        l.NoiDung,
                        SoLuotChon = _context.TraLoiKhaoSat.Count(t => t.MaLuaChon == l.MaLuaChon)
                    })
                }).ToListAsync();

            return Ok(new
            {
                khaoSat.MaKS,
                khaoSat.TenKhaoSat,
                TongSoPhieuNop = tongSoPhieu,
                ChiTietThongKe = thongKeCauHoi
            });
        }
    }

    public class NopKhaoSatDto
    {
        public int MaKS { get; set; }
        public int MaKH { get; set; }
        public List<TraLoiDto> CauTraLoi { get; set; } = new();
    }

    public class TraLoiDto
    {
        public int MaCauHoi { get; set; }
        public int? MaLuaChon { get; set; }
        public string? NoiDungTraLoi { get; set; }
        public int? SoSao { get; set; }
    }
}

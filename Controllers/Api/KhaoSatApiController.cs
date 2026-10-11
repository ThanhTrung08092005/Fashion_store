using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using System.Security.Claims;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KhaoSatApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public KhaoSatApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/KhaoSatApi - Danh sách các chiến dịch khảo sát
        [HttpGet]
        [Authorize(Roles = "Admin, Quản lý")]
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
                    SoPhieuDaNop = k.PhieuKhaoSats.Count(p => p.TrangThai == "Đã hoàn thành")
                })
                .ToListAsync();

            return Ok(list);
        }

        // 2. GET: api/KhaoSatApi/5/cau-hoi - Lấy toàn bộ câu hỏi và đáp án lựa chọn
        [HttpGet("{id}/cau-hoi")]
        [Authorize(Roles = "Admin, Quản lý, Khách hàng")]
        public async Task<IActionResult> GetCauHoi(int id)
        {
            if (User.IsInRole("Khách hàng"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int accountId)) return Unauthorized();
                var customerId = await _context.KhachHang
                    .Where(customer => customer.MaTK == accountId && !customer.DaXoa && customer.TaiKhoan != null && customer.TaiKhoan.TrangThai)
                    .Select(customer => (int?)customer.MaKH).FirstOrDefaultAsync();
                if (customerId == null || !await _context.PhieuKhaoSat.AnyAsync(ticket => ticket.MaKS == id && ticket.MaKH == customerId))
                    return Forbid();
            }

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
        [Authorize(Roles = "Khách hàng")]
        public async Task<IActionResult> NopBai([FromBody] NopKhaoSatDto model)
        {
            if (model.MaKS <= 0) return BadRequest(new { message = "Mã khảo sát không hợp lệ." });
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int accountId)) return Unauthorized();

            var customer = await _context.KhachHang.Include(item => item.TaiKhoan)
                .FirstOrDefaultAsync(item => item.MaTK == accountId && !item.DaXoa);
            if (customer?.TaiKhoan == null || !customer.TaiKhoan.TrangThai) return Unauthorized();

            var now = DateTime.Now;
            var ticket = await _context.PhieuKhaoSat.Include(item => item.KhaoSat)
                .FirstOrDefaultAsync(item => item.MaKS == model.MaKS && item.MaKH == customer.MaKH);
            if (ticket == null) return NotFound(new { message = "Không tìm thấy phiếu khảo sát đã phát cho tài khoản này." });
            if (ticket.TrangThai == "Đã hoàn thành") return Conflict(new { message = "Phiếu khảo sát này đã được nộp." });
            if (ticket.KhaoSat == null || !ticket.KhaoSat.TrangThai || ticket.KhaoSat.NgayBatDau > now || ticket.KhaoSat.NgayKetThuc < now)
                return BadRequest(new { message = "Khảo sát chưa mở hoặc đã đóng." });

            var questions = await _context.CauHoiKhaoSat.Include(question => question.LuaChonCauHois)
                .Where(question => question.MaKS == model.MaKS).ToListAsync();
            var answers = model.CauTraLoi ?? new List<TraLoiDto>();
            if (answers.GroupBy(answer => answer.MaCauHoi).Any(group => group.Count() > 1)
                || answers.Any(answer => questions.All(question => question.MaCauHoi != answer.MaCauHoi)))
                return BadRequest(new { message = "Câu trả lời có câu hỏi không hợp lệ hoặc bị lặp." });

            var answerEntities = new List<TraLoiKhaoSat>();
            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(item => item.MaCauHoi == question.MaCauHoi);
                if (question.LoaiCauHoi is "Một lựa chọn" or "Nhiều lựa chọn")
                {
                    if (!answer?.MaLuaChon.HasValue ?? true)
                    {
                        if (question.BatBuoc) return BadRequest(new { message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }
                    if (!question.LuaChonCauHois.Any(option => option.MaLuaChon == answer!.MaLuaChon))
                        return BadRequest(new { message = "Phương án trả lời không thuộc câu hỏi đã chọn." });
                    answerEntities.Add(new TraLoiKhaoSat { MaCauHoi = question.MaCauHoi, MaLuaChon = answer!.MaLuaChon, NgayNop = now });
                }
                else if (question.LoaiCauHoi == "Đánh giá sao")
                {
                    if (answer?.SoSao is null)
                    {
                        if (question.BatBuoc) return BadRequest(new { message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }
                    if (answer.SoSao is < 1 or > 5) return BadRequest(new { message = "Số sao phải nằm trong khoảng từ 1 đến 5." });
                    answerEntities.Add(new TraLoiKhaoSat { MaCauHoi = question.MaCauHoi, SoSao = answer.SoSao, NgayNop = now });
                }
                else if (question.LoaiCauHoi == "Tự luận")
                {
                    var content = answer?.NoiDungTraLoi?.Trim();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        if (question.BatBuoc) return BadRequest(new { message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }
                    if (content.Length > 4000) return BadRequest(new { message = "Câu trả lời không được vượt quá 4.000 ký tự." });
                    answerEntities.Add(new TraLoiKhaoSat { MaCauHoi = question.MaCauHoi, NoiDungTraLoi = content, NgayNop = now });
                }
                else return BadRequest(new { message = "Khảo sát chứa loại câu hỏi không được hỗ trợ." });
            }

            ticket.NgayNop = now;
            ticket.TrangThai = "Đã hoàn thành";
            foreach (var answer in answerEntities) answer.MaPhieuKS = ticket.MaPhieuKS;
            _context.TraLoiKhaoSat.AddRange(answerEntities);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nộp bài khảo sát thành công", maPhieuKS = ticket.MaPhieuKS, ngayNop = now });
        }

        // 4. GET: api/KhaoSatApi/5/thong-ke - Báo cáo thống kê kết quả khảo sát
        [HttpGet("{id}/thong-ke")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ThongKe(int id)
        {
            var khaoSat = await _context.KhaoSat
                .AsNoTracking()
                .Include(k => k.CauHoiKhaoSats)
                    .ThenInclude(c => c.LuaChonCauHois)
                .FirstOrDefaultAsync(k => k.MaKS == id);

            if (khaoSat == null) return NotFound(new { message = "Không tìm thấy khảo sát" });

            int tongSoPhieu = await _context.PhieuKhaoSat.CountAsync(p => p.MaKS == id && p.TrangThai == "Đã hoàn thành");

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
                        SoLuotChon = _context.TraLoiKhaoSat.Count(t => t.MaCauHoi == c.MaCauHoi && t.MaLuaChon == l.MaLuaChon)
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

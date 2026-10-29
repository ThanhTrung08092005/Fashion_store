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
    private static readonly string[] QuestionTypes = { "Một lựa chọn", "Nhiều lựa chọn", "Tự luận", "Đánh giá sao" };
    private readonly AppDbContext _db;

    public KhaoSatController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin,QuanLy")]
    public async Task<IActionResult> GetAll([FromQuery] QueryParams q)
    {
        var query = _db.KhaoSats.Include(k => k.CauHois).Include(k => k.PhieuKhaoSats).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(k => k.TenKhaoSat.Contains(q.Search));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(k => k.NgayBatDau)
            .Skip(Math.Max(0, q.Page - 1) * Math.Clamp(q.PageSize, 1, 100)).Take(Math.Clamp(q.PageSize, 1, 100))
            .Select(k => new KhaoSatDto(
                k.MaKS, k.MaQL, k.TenKhaoSat, k.MoTa, k.NgayBatDau, k.NgayKetThuc, k.TrangThai,
                k.CauHois.Count, k.PhieuKhaoSats.Count(p => p.TrangThai == "Đã hoàn thành"),
                k.PhieuKhaoSats.Count))
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<KhaoSatDto>>.Ok(new PagedResult<KhaoSatDto>
        {
            Items = items, TotalItems = total, Page = q.Page, PageSize = Math.Clamp(q.PageSize, 1, 100)
        }));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        if (!User.IsInRole("KhachHang")) return Forbid();
        if (!int.TryParse(User.FindFirst("maTK")?.Value, out var maTK)) return Unauthorized();

        var kh = await _db.KhachHangs.Include(k => k.TaiKhoan)
            .FirstOrDefaultAsync(k => k.MaTK == maTK && !k.DaXoa);
        if (kh == null || !kh.TaiKhoan.TrangThai) return Unauthorized(ApiResponse<object>.Fail("Tài khoản khách hàng không hoạt động."));

        var now = DateTime.Now;
        var eligibleSurveyIds = await _db.KhaoSats
            .Where(survey => survey.TrangThai && survey.NgayBatDau <= now && survey.NgayKetThuc >= now)
            .Select(survey => survey.MaKS)
            .ToListAsync();
        var assignedIds = await _db.PhieuKhaoSats.Where(ticket => ticket.MaKH == kh.MaKH)
            .Select(ticket => ticket.MaKS).ToListAsync();
        var missingTickets = eligibleSurveyIds.Except(assignedIds).Select(surveyId => new PhieuKhaoSat
        {
            MaKS = surveyId,
            MaKH = kh.MaKH,
            NgayBatDau = now,
            TrangThai = "Đang làm"
        }).ToList();
        if (missingTickets.Count > 0)
        {
            _db.PhieuKhaoSats.AddRange(missingTickets);
            await _db.SaveChangesAsync();
        }

        var tickets = await _db.PhieuKhaoSats
            .AsNoTracking()
            .Include(ticket => ticket.KhaoSat).ThenInclude(survey => survey.CauHois).ThenInclude(question => question.LuaChons)
            .Include(ticket => ticket.TraLois)
            .Where(ticket => ticket.MaKH == kh.MaKH)
            .OrderByDescending(ticket => ticket.KhaoSat.NgayBatDau)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(tickets.Select(ticket => new
        {
            ticket.MaPhieuKS,
            ticket.TrangThai,
            IsOpen = ticket.KhaoSat.TrangThai && ticket.KhaoSat.NgayBatDau <= now && ticket.KhaoSat.NgayKetThuc >= now,
            KhaoSat = new KhaoSatDto(
                ticket.KhaoSat.MaKS, ticket.KhaoSat.MaQL, ticket.KhaoSat.TenKhaoSat, ticket.KhaoSat.MoTa,
                ticket.KhaoSat.NgayBatDau, ticket.KhaoSat.NgayKetThuc, ticket.KhaoSat.TrangThai,
                ticket.KhaoSat.CauHois.Count,
                ticket.KhaoSat.PhieuKhaoSats.Count(x => x.TrangThai == "Đã hoàn thành")),
            CauHois = ticket.KhaoSat.CauHois.OrderBy(question => question.ThuTu).Select(question => new
            {
                question.MaCauHoi, question.NoiDung, question.LoaiCauHoi, question.BatBuoc,
                LuaChons = question.LuaChons.OrderBy(option => option.ThuTu).Select(option => new { option.MaLuaChon, option.NoiDung }),
                TraLois = ticket.TraLois.Where(answer => answer.MaCauHoi == question.MaCauHoi)
                    .Select(answer => new { answer.MaLuaChon, answer.NoiDungTraLoi, answer.SoSao })
            })
        }).ToList()));
    }

    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var survey = await _db.KhaoSats
            .Include(item => item.CauHois).ThenInclude(question => question.LuaChons)
            .FirstOrDefaultAsync(item => item.MaKS == id);
        if (survey == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khảo sát."));

        if (User.IsInRole("KhachHang"))
        {
            if (!int.TryParse(User.FindFirst("maTK")?.Value, out var maTK)) return Unauthorized();
            var customerId = await _db.KhachHangs.Where(customer => customer.MaTK == maTK && !customer.DaXoa)
                .Select(customer => (int?)customer.MaKH).FirstOrDefaultAsync();
            if (customerId == null || !await _db.PhieuKhaoSats.AnyAsync(ticket => ticket.MaKS == id && ticket.MaKH == customerId))
                return Forbid();
        }
        else if (!User.IsInRole("Admin") && !User.IsInRole("QuanLy")) return Forbid();

        return Ok(ApiResponse<object>.Ok(new
        {
            survey.MaKS, survey.TenKhaoSat, survey.MoTa, survey.NgayBatDau, survey.NgayKetThuc,
            survey.TrangThai,
            CauHois = survey.CauHois.OrderBy(question => question.ThuTu).Select(question => new
            {
                question.MaCauHoi, question.NoiDung, question.LoaiCauHoi, question.ThuTu, question.BatBuoc,
                LuaChons = question.LuaChons.OrderBy(option => option.ThuTu).Select(option => new { option.MaLuaChon, option.NoiDung })
            })
        }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] KhaoSatCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.TenKhaoSat) || dto.TenKhaoSat.Trim().Length > 200)
            return BadRequest(ApiResponse<object>.Fail("Tên khảo sát bắt buộc và không quá 200 ký tự."));
        if (dto.NgayBatDau < DateTime.Now.AddMinutes(-1) || dto.NgayKetThuc <= dto.NgayBatDau)
            return BadRequest(ApiResponse<object>.Fail("Thời gian khảo sát không hợp lệ."));
        if (dto.CauHois == null || dto.CauHois.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Bài khảo sát cần ít nhất một câu hỏi."));

        foreach (var question in dto.CauHois)
        {
            if (question == null || string.IsNullOrWhiteSpace(question.NoiDung) || question.NoiDung.Trim().Length > 1000
                || !QuestionTypes.Contains(question.LoaiCauHoi, StringComparer.Ordinal))
                return BadRequest(ApiResponse<object>.Fail("Câu hỏi thiếu nội dung hoặc có loại không hợp lệ."));
            if (question.LoaiCauHoi is "Một lựa chọn" or "Nhiều lựa chọn")
            {
                var options = question.LuaChons?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? new();
                if (options.Count < 2 || options.Count > 100 || options.Any(value => value.Length > 500)
                    || options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
                    return BadRequest(ApiResponse<object>.Fail("Câu hỏi lựa chọn cần ít nhất hai phương án khác nhau."));
            }
        }

        if (!int.TryParse(User.FindFirst("maTK")?.Value, out var accountId)) return Unauthorized();
        var manager = await _db.QuanLys.FirstOrDefaultAsync(item => item.MaTK == accountId);
        if (manager == null) return Forbid();

        var survey = new KhaoSat
        {
            MaQL = manager.MaQL,
            TenKhaoSat = dto.TenKhaoSat.Trim(),
            MoTa = string.IsNullOrWhiteSpace(dto.MoTa) ? null : dto.MoTa.Trim(),
            NgayBatDau = dto.NgayBatDau,
            NgayKetThuc = dto.NgayKetThuc,
            TrangThai = true,
            CauHois = dto.CauHois.Select((question, index) => new CauHoiKhaoSat
            {
                NoiDung = question.NoiDung.Trim(),
                LoaiCauHoi = question.LoaiCauHoi,
                ThuTu = question.ThuTu > 0 ? question.ThuTu : index + 1,
                BatBuoc = question.BatBuoc,
                LuaChons = (question.LuaChons ?? new()).Where(option => !string.IsNullOrWhiteSpace(option)).Select(option => option.Trim()).Select((option, optionIndex) => new LuaChonCauHoi
                {
                    NoiDung = option.Trim(),
                    ThuTu = optionIndex + 1
                }).ToList()
            }).ToList()
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.KhaoSats.Add(survey);
        await _db.SaveChangesAsync();

        var activeCustomers = await _db.KhachHangs
            .Where(customer => !customer.DaXoa && customer.TaiKhoan.TrangThai && customer.TaiKhoan.MaVaiTro == 2)
            .Select(customer => customer.MaKH)
            .ToListAsync();
        _db.PhieuKhaoSats.AddRange(activeCustomers.Select(customerId => new PhieuKhaoSat
        {
            MaKS = survey.MaKS,
            MaKH = customerId,
            NgayBatDau = DateTime.Now,
            TrangThai = "Đang làm"
        }));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var message = $"Đã tạo khảo sát và phát phiếu cho {activeCustomers.Count} khách hàng đang hoạt động.";
        return Ok(ApiResponse<object>.Ok(new { survey.MaKS }, message));
    }

    [HttpPost("{id:int}/send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendToCustomers(int id, [FromBody] List<int>? maKHList)
    {
        var survey = await _db.KhaoSats.FirstOrDefaultAsync(item => item.MaKS == id);
        if (survey == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khảo sát."));
        if (!survey.TrangThai || survey.NgayKetThuc < DateTime.Now)
            return BadRequest(ApiResponse<object>.Fail("Không thể phát phiếu cho khảo sát đã đóng hoặc hết hạn."));

        var selected = (maKHList ?? new()).Distinct().ToList();
        var customers = await _db.KhachHangs
            .Where(customer => selected.Contains(customer.MaKH) && !customer.DaXoa
                && customer.TaiKhoan.TrangThai && customer.TaiKhoan.MaVaiTro == 2)
            .Select(customer => customer.MaKH).ToListAsync();
        var existing = await _db.PhieuKhaoSats.Where(ticket => ticket.MaKS == id && customers.Contains(ticket.MaKH))
            .Select(ticket => ticket.MaKH).ToListAsync();
        var newIds = customers.Except(existing).ToList();
        _db.PhieuKhaoSats.AddRange(newIds.Select(customerId => new PhieuKhaoSat
        {
            MaKS = id, MaKH = customerId, NgayBatDau = DateTime.Now, TrangThai = "Đang làm"
        }));
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Sent = newIds.Count }, $"Đã phát phiếu cho {newIds.Count} khách hàng đang hoạt động."));
    }

    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Close(int id)
    {
        var survey = await _db.KhaoSats.FindAsync(id);
        if (survey == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khảo sát."));
        survey.TrangThai = false;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { survey.MaKS }, "Đã đóng khảo sát."));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitSurveyDto dto)
    {
        if (!User.IsInRole("KhachHang")) return Forbid();
        if (dto == null || dto.MaPhieuKS <= 0 || dto.TraLois == null)
            return BadRequest(ApiResponse<object>.Fail("Dữ liệu câu trả lời không hợp lệ."));
        if (!int.TryParse(User.FindFirst("maTK")?.Value, out var maTK)) return Unauthorized();

        var customer = await _db.KhachHangs.Include(item => item.TaiKhoan)
            .FirstOrDefaultAsync(item => item.MaTK == maTK && !item.DaXoa);
        if (customer == null || !customer.TaiKhoan.TrangThai) return Unauthorized(ApiResponse<object>.Fail("Tài khoản khách hàng không hoạt động."));

        var ticket = await _db.PhieuKhaoSats
            .Include(item => item.KhaoSat).ThenInclude(survey => survey.CauHois).ThenInclude(question => question.LuaChons)
            .FirstOrDefaultAsync(item => item.MaPhieuKS == dto.MaPhieuKS && item.MaKH == customer.MaKH);
        if (ticket == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phiếu khảo sát của bạn."));
        if (ticket.TrangThai == "Đã hoàn thành") return BadRequest(ApiResponse<object>.Fail("Phiếu đã được nộp trước đó."));
        var now = DateTime.Now;
        if (!ticket.KhaoSat.TrangThai || ticket.KhaoSat.NgayBatDau > now || ticket.KhaoSat.NgayKetThuc < now)
            return BadRequest(ApiResponse<object>.Fail("Khảo sát chưa mở hoặc đã đóng/hết hạn."));

        var questions = ticket.KhaoSat.CauHois.ToList();
        var unknownQuestionIds = dto.TraLois.Select(answer => answer.MaCauHoi).Distinct()
            .Except(questions.Select(question => question.MaCauHoi)).ToList();
        if (unknownQuestionIds.Count > 0) return BadRequest(ApiResponse<object>.Fail("Có câu hỏi không thuộc khảo sát này."));

        foreach (var question in questions)
        {
            var answers = dto.TraLois.Where(answer => answer.MaCauHoi == question.MaCauHoi).ToList();
            if (question.LoaiCauHoi == "Một lựa chọn")
            {
                if (answers.Count > 1 || (question.BatBuoc && (answers.Count == 0 || !answers[0].MaLuaChon.HasValue)))
                    return BadRequest(ApiResponse<object>.Fail("Câu hỏi một lựa chọn cần đúng một phương án."));
                if (answers.Any(answer => !question.LuaChons.Any(option => option.MaLuaChon == answer.MaLuaChon)))
                    return BadRequest(ApiResponse<object>.Fail("Phương án không thuộc câu hỏi đã chọn."));
            }
            else if (question.LoaiCauHoi == "Nhiều lựa chọn")
            {
                if (question.BatBuoc && answers.Count == 0)
                    return BadRequest(ApiResponse<object>.Fail("Vui lòng trả lời đầy đủ các câu hỏi bắt buộc."));
                if (answers.Any(answer => !answer.MaLuaChon.HasValue || !question.LuaChons.Any(option => option.MaLuaChon == answer.MaLuaChon))
                    || answers.Select(answer => answer.MaLuaChon).Distinct().Count() != answers.Count)
                    return BadRequest(ApiResponse<object>.Fail("Có phương án nhiều lựa chọn không hợp lệ."));
            }
            else if (question.LoaiCauHoi == "Đánh giá sao")
            {
                if (answers.Count > 1 || (question.BatBuoc && answers.Count == 0)
                    || answers.Any(answer => !answer.SoSao.HasValue || answer.SoSao < 1 || answer.SoSao > 5))
                    return BadRequest(ApiResponse<object>.Fail("Đánh giá sao phải từ 1 đến 5."));
            }
            else if (question.LoaiCauHoi == "Tự luận")
            {
                if (answers.Count > 1 || (question.BatBuoc && string.IsNullOrWhiteSpace(answers.FirstOrDefault()?.NoiDungTraLoi))
                    || answers.Any(answer => (answer.NoiDungTraLoi?.Length ?? 0) > 4000))
                    return BadRequest(ApiResponse<object>.Fail("Câu trả lời tự luận không hợp lệ hoặc vượt quá 4.000 ký tự."));
            }
        }

        _db.TraLoiKhaoSats.AddRange(dto.TraLois.Select(answer => new TraLoiKhaoSat
        {
            MaPhieuKS = ticket.MaPhieuKS,
            MaCauHoi = answer.MaCauHoi,
            MaLuaChon = answer.MaLuaChon,
            NoiDungTraLoi = answer.NoiDungTraLoi?.Trim(),
            SoSao = answer.SoSao,
            NgayNop = now
        }));
        ticket.TrangThai = "Đã hoàn thành";
        ticket.NgayNop = now;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Nộp khảo sát thành công."));
    }
}

public record SubmitSurveyDto(int MaPhieuKS, List<TraLoiDto> TraLois);
public record TraLoiDto(int MaCauHoi, int? MaLuaChon, string? NoiDungTraLoi, int? SoSao);

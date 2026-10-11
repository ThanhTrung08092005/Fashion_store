using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using Fashion_store.Services;

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Quản lý, Admin")]
    public class SurveyController : Controller
    {
        private readonly FashionStoreDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SurveyController(FashionStoreDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==============================================================================
        // 1. HIỂN THỊ GIAO DIỆN KHẢO SÁT & PHẢN HỒI (TRANG 2)
        // ==============================================================================
        [HttpGet]
        public async Task<IActionResult> SurveyFeedback()
        {
            await SurveyExpirationService.CloseExpiredSurveysAsync(_context, HttpContext.RequestAborted);

            ViewBag.IsAdmin = User.IsInRole("Admin") || User.HasClaim("MaVaiTro", "3");
            if (!ViewBag.IsAdmin)
            {
                var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(accountIdValue, out int accountId))
                {
                    ViewBag.CurrentManagerId = await _context.QuanLy
                        .Where(manager => manager.MaTK == accountId)
                        .Select(manager => (int?)manager.MaQL)
                        .FirstOrDefaultAsync();
                }
            }

            ViewBag.Surveys = await _context.KhaoSat
                .Include(k => k.CauHoiKhaoSats)
                    .ThenInclude(c => c.LuaChonCauHois)
                .Include(k => k.PhieuKhaoSats)
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();

            ViewBag.Feedbacks = await _context.PhanHoi
                .Include(p => p.KhachHang)
                .Include(p => p.SanPham)
                .OrderByDescending(p => p.NgayGui)
                .ToListAsync();

            ViewBag.Products = await _context.SanPham.Where(s => s.TrangThai).ToListAsync();

            return View("~/Views/Admin/Survey/SurveyFeedback.cshtml");
        }

        // ==============================================================================
        // 2. BACKEND LƯU BÀI KHẢO SÁT VÀO CSDL VÀ PHÁT PHIẾU TỚI TOÀN BỘ KHÁCH HÀNG
        // ==============================================================================
        [HttpPost]
        [Authorize(Roles = "Quản lý, Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSurveyApi([FromBody] CreateSurveyRequestDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.TenKhaoSat))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên bài khảo sát." });
            }

            var allowedQuestionTypes = new[] { "Một lựa chọn", "Nhiều lựa chọn", "Tự luận", "Đánh giá sao" };
            if (model.TenKhaoSat.Trim().Length > 200)
            {
                return Json(new { success = false, message = "Tên bài khảo sát không được vượt quá 200 ký tự." });
            }

            if (model.NgayBatDau == default || model.NgayKetThuc == default
                || model.NgayBatDau < DateTime.Now.AddMinutes(-1)
                || model.NgayKetThuc <= model.NgayBatDau)
            {
                return Json(new { success = false, message = "Vui lòng chọn thời gian bắt đầu từ hiện tại trở đi và kết thúc sau thời gian bắt đầu." });
            }

            if (model.Questions == null || model.Questions.Count == 0)
            {
                return Json(new { success = false, message = "Bài khảo sát cần ít nhất một câu hỏi." });
            }

            foreach (var question in model.Questions)
            {
                if (question == null || string.IsNullOrWhiteSpace(question.NoiDung)
                    || question.NoiDung.Trim().Length > 1000
                    || !allowedQuestionTypes.Contains(question.LoaiCauHoi, StringComparer.Ordinal))
                {
                    return Json(new { success = false, message = "Có câu hỏi thiếu nội dung, quá dài hoặc có loại câu hỏi không hợp lệ." });
                }

                if (question.LoaiCauHoi == "Một lựa chọn" || question.LoaiCauHoi == "Nhiều lựa chọn")
                {
                    var options = question.Options?
                        .Where(option => !string.IsNullOrWhiteSpace(option))
                        .Select(option => option.Trim())
                        .ToList() ?? new List<string>();

                    if (options.Count < 2 || options.Any(option => option.Length > 500)
                        || options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
                    {
                        return Json(new { success = false, message = "Mỗi câu hỏi lựa chọn cần ít nhất hai phương án khác nhau, tối đa 500 ký tự mỗi phương án." });
                    }

                    question.Options = options;
                }
            }

            var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountIdValue, out int maTK))
            {
                return Forbid();
            }

            var quanLy = await _context.QuanLy.FirstOrDefaultAsync(q => q.MaTK == maTK);
            if (quanLy == null)
            {
                return Forbid();
            }

            var questions = model.Questions.Select((question, index) => new CauHoiKhaoSat
            {
                NoiDung = question.NoiDung.Trim(),
                LoaiCauHoi = question.LoaiCauHoi,
                ThuTu = index + 1,
                BatBuoc = true,
                LuaChonCauHois = (question.Options ?? new List<string>())
                    .Where(option => !string.IsNullOrWhiteSpace(option))
                    .Select((option, optionIndex) => new LuaChonCauHoi { NoiDung = option.Trim(), ThuTu = optionIndex + 1 })
                    .ToList()
            }).ToList();

            var khaoSat = new KhaoSat
            {
                MaQL = quanLy.MaQL,
                TenKhaoSat = model.TenKhaoSat.Trim(),
                MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc,
                TrangThai = true,
                CauHoiKhaoSats = questions
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.KhaoSat.Add(khaoSat);
            await _context.SaveChangesAsync();

            var activeCustomers = await _context.KhachHang
                .Where(k => !k.DaXoa
                    && k.TaiKhoan != null
                    && k.TaiKhoan.TrangThai
                    && k.TaiKhoan.MaVaiTro == 2)
                .ToListAsync();
            foreach (var kh in activeCustomers)
            {
                _context.PhieuKhaoSat.Add(new PhieuKhaoSat
                {
                    MaKS = khaoSat.MaKS,
                    MaKH = kh.MaKH,
                    NgayBatDau = DateTime.Now,
                    TrangThai = "Đang làm"
                });
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = $"Đã tạo khảo sát; phiếu sẽ hiện cho {activeCustomers.Count} khách hàng đang hoạt động khi tới giờ bắt đầu.";
            return Json(new { success = true, message });
        }

        [HttpPost]
        [Authorize(Roles = "Quản lý, Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseSurvey(int id)
        {
            var survey = await _context.KhaoSat.FirstOrDefaultAsync(item => item.MaKS == id);
            if (survey == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khảo sát cần đóng.";
                return RedirectToAction(nameof(SurveyFeedback));
            }

            if (!User.IsInRole("Admin") && !User.HasClaim("MaVaiTro", "3"))
            {
                var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdValue, out int accountId))
                    return Forbid();

                var managerId = await _context.QuanLy
                    .Where(manager => manager.MaTK == accountId)
                    .Select(manager => (int?)manager.MaQL)
                    .FirstOrDefaultAsync();
                if (managerId == null || survey.MaQL != managerId.Value)
                    return Forbid();
            }

            survey.TrangThai = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đóng khảo sát.";
            return RedirectToAction(nameof(SurveyFeedback));
        }

        // ============================================================================== 
        // 4. BACKEND QUẢN LÝ PHÚC ĐÁP Ý KIẾN KHÁCH HÀNG
        // ==============================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyFeedback(int MaPH, string PhanHoiQuanLy)
        {
            var ph = await _context.PhanHoi.FindAsync(MaPH);
            if (ph == null || string.IsNullOrWhiteSpace(PhanHoiQuanLy))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phản hồi hoặc nội dung phúc đáp đang trống.";
                return RedirectToAction(nameof(SurveyFeedback));
            }

            if (PhanHoiQuanLy.Trim().Length > 2000)
            {
                TempData["ErrorMessage"] = "Nội dung phúc đáp không được vượt quá 2.000 ký tự.";
                return RedirectToAction(nameof(SurveyFeedback));
            }

            if (!string.IsNullOrWhiteSpace(ph.PhanHoiQuanLy) || ph.NgayXuLy.HasValue || ph.TrangThai == "Đã xử lý")
            {
                TempData["ErrorMessage"] = "Phản hồi này đã được phúc đáp. Mỗi phản hồi chỉ được trả lời một lần.";
                return RedirectToAction(nameof(SurveyFeedback));
            }

            var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountIdValue, out int maTK))
            {
                return Forbid();
            }

            var quanLy = await _context.QuanLy.FirstOrDefaultAsync(q => q.MaTK == maTK);
            if (quanLy == null)
            {
                return Forbid();
            }

            var reply = PhanHoiQuanLy.Trim();
            var now = DateTime.Now;
            var updated = await _context.PhanHoi
                .Where(item => item.MaPH == MaPH
                    && (item.PhanHoiQuanLy == null || item.PhanHoiQuanLy == "")
                    && item.NgayXuLy == null
                    && item.TrangThai != "Đã xử lý")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.MaQL, (int?)quanLy.MaQL)
                    .SetProperty(item => item.PhanHoiQuanLy, reply)
                    .SetProperty(item => item.TrangThai, "Đã xử lý")
                    .SetProperty(item => item.NgayXuLy, (DateTime?)now));

            if (updated == 0)
            {
                TempData["ErrorMessage"] = "Phản hồi này vừa được phúc đáp bởi một quản lý khác.";
                return RedirectToAction(nameof(SurveyFeedback));
            }
            TempData["SuccessMessage"] = "Đã gửi phúc đáp đến khách hàng thành công!";
            return RedirectToAction(nameof(SurveyFeedback));
        }

        // ==============================================================================
        // 5. TRANG 3: BÁO CÁO THỐNG KÊ CRM TÍNH TOÁN 100% TỪ CSDL
        // ==============================================================================
        [HttpGet]
        [Authorize(Roles = "Quản lý, Admin")]
        public async Task<IActionResult> Analytics()
        {
            // A. Đếm tổng số khách hàng
            int totalCustomers = await _context.KhachHang
                .CountAsync(customer => !customer.DaXoa && customer.TaiKhoan != null && customer.TaiKhoan.MaVaiTro == 2);
            ViewBag.TotalCustomers = totalCustomers;

            // B. Tính tỷ lệ nhóm tuổi
            var customerBirthDates = await _context.KhachHang
                .Where(k => !k.DaXoa && k.TaiKhoan != null && k.TaiKhoan.MaVaiTro == 2
                    && k.NgaySinh.HasValue && k.NgaySinh.Value <= DateTime.Today)
                .Select(k => k.NgaySinh!.Value)
                .ToListAsync();

            var today = DateTime.Today;
            int countUnder18 = 0, count18To24 = 0, count25To35 = 0, countOver35 = 0;

            foreach (var birthDate in customerBirthDates)
            {
                int age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age)) age--;

                if (age < 18) countUnder18++;
                else if (age <= 24) count18To24++;
                else if (age <= 35) count25To35++;
                else countOver35++;
            }

            int ageDataCount = customerBirthDates.Count;
            double pctUnder18 = ageDataCount == 0 ? 0 : Math.Round((double)countUnder18 * 100.0 / ageDataCount, 1);
            double pct18To24  = ageDataCount == 0 ? 0 : Math.Round((double)count18To24 * 100.0 / ageDataCount, 1);
            double pct25To35  = ageDataCount == 0 ? 0 : Math.Round((double)count25To35 * 100.0 / ageDataCount, 1);
            double pctOver35  = ageDataCount == 0 ? 0 : Math.Round((double)countOver35 * 100.0 / ageDataCount, 1);

            ViewBag.AgeLabels = new string[] { "Dưới 18 tuổi", "18 - 24 tuổi", "25 - 35 tuổi", "Trên 35 tuổi" };
            ViewBag.AgePercents = new double[] { pctUnder18, pct18To24, pct25To35, pctOver35 };
            ViewBag.AgeDataCount = ageDataCount;

            if (ageDataCount > 0)
            {
                var ageGroups = new[]
                {
                    (Name: "Dưới 18 tuổi", Percent: pctUnder18),
                    (Name: "18 - 24 tuổi", Percent: pct18To24),
                    (Name: "25 - 35 tuổi", Percent: pct25To35),
                    (Name: "Trên 35 tuổi", Percent: pctOver35)
                };
                var topAge = ageGroups.OrderByDescending(group => group.Percent).First();
                ViewBag.TopAgeGroup = topAge.Name;
                ViewBag.TopAgePercent = topAge.Percent;
            }
            else
            {
                ViewBag.TopAgeGroup = "Chưa có dữ liệu";
                ViewBag.TopAgePercent = 0;
            }

            // C. Lấy 100% danh mục sở thích từ CSDL
            var styleLabels = new List<string>();
            var stylePercents = new List<double>();
            string? topStyleName = null;
            double maxStyleVal = 0;
            bool hasStyleData = false;
            string styleDataMessage = "Chưa có dữ liệu sở thích khách hàng để thống kê.";
            var connection = _context.Database.GetDbConnection();
            bool connectionWasOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!connectionWasOpen)
                    await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        dm.TenDM,
                        COUNT(DISTINCT CASE WHEN kh.DaXoa = 0 AND tk.MaVaiTro = 2 THEN ks.MaKH END) AS SoLuong
                    FROM DANHMUC dm
                    LEFT JOIN KHACHHANG_SOTHICH ks ON dm.MaDM = ks.MaDM
                    LEFT JOIN KHACHHANG kh ON kh.MaKH = ks.MaKH
                    LEFT JOIN TAIKHOAN tk ON tk.MaTK = kh.MaTK
                    WHERE dm.TrangThai = 1
                    GROUP BY dm.MaDM, dm.TenDM
                    ORDER BY SoLuong DESC";

                using var reader = await cmd.ExecuteReaderAsync();
                var allStyles = new List<(string Name, int Count)>();

                while (await reader.ReadAsync())
                {
                    allStyles.Add((reader.GetString(0), reader.GetInt32(1)));
                }

                if (allStyles.Any(item => item.Count > 0))
                {
                    foreach (var item in allStyles)
                    {
                        double pct = totalCustomers == 0 ? 0 : Math.Round((double)item.Count * 100.0 / totalCustomers, 1);
                        styleLabels.Add(item.Name);
                        stylePercents.Add(pct);
                    }

                    topStyleName = allStyles[0].Name;
                    maxStyleVal = stylePercents[0];
                    hasStyleData = true;
                    styleDataMessage = string.Empty;
                }
            }
            catch
            {
                styleLabels.Clear();
                stylePercents.Clear();
                topStyleName = null;
                maxStyleVal = 0;
                styleDataMessage = "Không thể truy vấn dữ liệu sở thích khách hàng. Vui lòng kiểm tra dữ liệu và kết nối cơ sở dữ liệu.";
            }
            finally
            {
                if (!connectionWasOpen && connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            ViewBag.StyleLabels = styleLabels.ToArray();
            ViewBag.StylePercents = stylePercents.ToArray();
            ViewBag.TopStyle = topStyleName;
            ViewBag.TopStylePercent = maxStyleVal;
            ViewBag.HasStyleData = hasStyleData;
            ViewBag.StyleDataMessage = styleDataMessage;

            // D. Các khảo sát đã đóng hoặc hết hạn có thể thống kê.
            var now = DateTime.Now;
            ViewBag.ClosedSurveys = await _context.KhaoSat
                .Where(survey => !survey.TrangThai || survey.NgayKetThuc < now)
                .Include(survey => survey.PhieuKhaoSats)
                .OrderByDescending(survey => survey.NgayBatDau)
                .ToListAsync();

            return View("~/Views/Admin/Survey/Analytics.cshtml");
        }

        [HttpGet]
        [Authorize(Roles = "Quản lý, Admin")]
        public async Task<IActionResult> Statistics(int id)
        {
            var statistics = await BuildStatisticsAsync(id);
            if (statistics == null) return NotFound();
            if (statistics.NgayKetThuc >= DateTime.Now
                && await _context.KhaoSat.AnyAsync(survey => survey.MaKS == id && survey.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể thống kê khảo sát đã đóng hoặc hết hạn.";
                return RedirectToAction(nameof(Analytics));
            }

            return View("~/Views/Admin/Survey/Statistics.cshtml", statistics);
        }

        [HttpGet]
        [Authorize(Roles = "Quản lý, Admin")]
        public async Task<IActionResult> ExportStatistics(int id)
        {
            var statistics = await BuildStatisticsAsync(id);
            if (statistics == null) return NotFound();
            if (statistics.NgayKetThuc >= DateTime.Now
                && await _context.KhaoSat.AnyAsync(survey => survey.MaKS == id && survey.TrangThai))
                return BadRequest("Chỉ có thể xuất thống kê khảo sát đã đóng hoặc hết hạn.");

            var reportDirectory = Path.Combine(_environment.ContentRootPath, "App_Data", "SurveyReports");
            Directory.CreateDirectory(reportDirectory);
            var reportPath = Path.Combine(reportDirectory, $"survey-{id}.xlsx");
            var reportBytes = SurveyExcelReportBuilder.Create(statistics);
            var temporaryPath = Path.Combine(reportDirectory, $"survey-{id}-{Guid.NewGuid():N}.tmp");
            try
            {
                await System.IO.File.WriteAllBytesAsync(temporaryPath, reportBytes, HttpContext.RequestAborted);
                System.IO.File.Move(temporaryPath, reportPath, overwrite: true);
            }
            finally
            {
                if (System.IO.File.Exists(temporaryPath)) System.IO.File.Delete(temporaryPath);
            }

            return PhysicalFile(reportPath,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"khaosat-{id}-thong-ke.xlsx");
        }

        private async Task<SurveyStatisticsViewModel?> BuildStatisticsAsync(int surveyId)
        {
            var survey = await _context.KhaoSat.AsNoTracking()
                .Include(item => item.CauHoiKhaoSats).ThenInclude(question => question.LuaChonCauHois)
                .Include(item => item.PhieuKhaoSats).ThenInclude(ticket => ticket.TraLoiKhaoSats)
                .FirstOrDefaultAsync(item => item.MaKS == surveyId);
            if (survey == null) return null;

            var completedTickets = survey.PhieuKhaoSats
                .Where(ticket => ticket.TrangThai == "Đã hoàn thành")
                .ToList();
            var questions = new List<SurveyQuestionStatisticsViewModel>();

            foreach (var question in survey.CauHoiKhaoSats.OrderBy(item => item.ThuTu))
            {
                var answers = completedTickets
                    .SelectMany(ticket => ticket.TraLoiKhaoSats
                        .Where(answer => answer.MaCauHoi == question.MaCauHoi)
                        .Select(answer => (Ticket: ticket, Answer: answer)))
                    .ToList();

                if (question.LoaiCauHoi is "Một lựa chọn" or "Nhiều lựa chọn")
                {
                    var respondentCount = answers.Select(row => row.Ticket.MaPhieuKS).Distinct().Count();
                    questions.Add(new SurveyQuestionStatisticsViewModel
                    {
                        MaCauHoi = question.MaCauHoi,
                        ThuTu = question.ThuTu,
                        NoiDung = question.NoiDung,
                        LoaiCauHoi = question.LoaiCauHoi,
                        SoNguoiTraLoi = respondentCount,
                        LuaChons = question.LuaChonCauHois.OrderBy(option => option.ThuTu).Select(option =>
                        {
                            var count = answers.Where(row => row.Answer.MaLuaChon == option.MaLuaChon)
                                .Select(row => row.Ticket.MaPhieuKS).Distinct().Count();
                            return new SurveyOptionStatisticsViewModel
                            {
                                MaLuaChon = option.MaLuaChon,
                                NoiDung = option.NoiDung,
                                SoLuotChon = count,
                                TyLe = respondentCount == 0 ? 0 : (double)count * 100 / respondentCount
                            };
                        }).ToList()
                    });
                }
                else if (question.LoaiCauHoi == "Đánh giá sao")
                {
                    var starAnswers = answers.Where(row => row.Answer.SoSao is >= 1 and <= 5).ToList();
                    var respondentCount = starAnswers.Select(row => row.Ticket.MaPhieuKS).Distinct().Count();
                    questions.Add(new SurveyQuestionStatisticsViewModel
                    {
                        MaCauHoi = question.MaCauHoi,
                        ThuTu = question.ThuTu,
                        NoiDung = question.NoiDung,
                        LoaiCauHoi = question.LoaiCauHoi,
                        SoNguoiTraLoi = respondentCount,
                        DiemTrungBinh = respondentCount == 0 ? null : starAnswers.Average(row => row.Answer.SoSao!.Value),
                        LuaChons = Enumerable.Range(1, 5).Select(star =>
                        {
                            var count = starAnswers.Where(row => row.Answer.SoSao == star)
                                .Select(row => row.Ticket.MaPhieuKS).Distinct().Count();
                            return new SurveyOptionStatisticsViewModel
                            {
                                NoiDung = $"{star} sao",
                                SoLuotChon = count,
                                TyLe = respondentCount == 0 ? 0 : (double)count * 100 / respondentCount
                            };
                        }).ToList()
                    });
                }
                else
                {
                    var textAnswers = answers
                        .Where(row => !string.IsNullOrWhiteSpace(row.Answer.NoiDungTraLoi))
                        .OrderBy(row => row.Ticket.NgayNop)
                        .Select(row => new SurveyTextAnswerViewModel
                        {
                            MaPhieuKS = row.Ticket.MaPhieuKS,
                            NgayNop = row.Answer.NgayNop ?? row.Ticket.NgayNop,
                            NoiDung = row.Answer.NoiDungTraLoi!.Trim()
                        }).ToList();
                    questions.Add(new SurveyQuestionStatisticsViewModel
                    {
                        MaCauHoi = question.MaCauHoi,
                        ThuTu = question.ThuTu,
                        NoiDung = question.NoiDung,
                        LoaiCauHoi = question.LoaiCauHoi,
                        SoNguoiTraLoi = textAnswers.Select(answer => answer.MaPhieuKS).Distinct().Count(),
                        TraLoiTuLuan = textAnswers
                    });
                }
            }

            return new SurveyStatisticsViewModel
            {
                MaKS = survey.MaKS,
                TenKhaoSat = survey.TenKhaoSat,
                MoTa = survey.MoTa,
                NgayBatDau = survey.NgayBatDau,
                NgayKetThuc = survey.NgayKetThuc,
                TongSoPhieu = survey.PhieuKhaoSats.Count,
                SoPhieuHoanThanh = completedTickets.Count,
                CauHoi = questions
            };
        }
    }
    

    public class CreateSurveyRequestDto
    {
        public string TenKhaoSat { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public List<QuestionItemDto> Questions { get; set; } = new();
    }

    public class QuestionItemDto
    {
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiCauHoi { get; set; } = "Một lựa chọn";
        public List<string> Options { get; set; } = new();
    }

}

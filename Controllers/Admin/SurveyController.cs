using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Quản lý")]
    public class SurveyController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public SurveyController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // ==============================================================================
        // 1. HIỂN THỊ GIAO DIỆN KHẢO SÁT & PHẢN HỒI (TRANG 2)
        // ==============================================================================
        [HttpGet]
        public async Task<IActionResult> SurveyFeedback()
        {
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
                || model.NgayBatDau.Date < DateTime.Today
                || model.NgayKetThuc.Date < model.NgayBatDau.Date)
            {
                return Json(new { success = false, message = "Vui lòng chọn thời gian hợp lệ; ngày kết thúc phải bằng hoặc sau ngày bắt đầu." });
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

            var khaoSat = new KhaoSat
            {
                MaQL = quanLy.MaQL,
                TenKhaoSat = model.TenKhaoSat.Trim(),
                MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                NgayBatDau = model.NgayBatDau.Date,
                NgayKetThuc = model.NgayKetThuc.Date,
                TrangThai = true
            };
            _context.KhaoSat.Add(khaoSat);
            await _context.SaveChangesAsync();

            int qOrder = 1;
            foreach (var q in model.Questions)
            {
                var cauHoi = new CauHoiKhaoSat
                {
                    MaKS = khaoSat.MaKS,
                    NoiDung = q.NoiDung.Trim(),
                    LoaiCauHoi = q.LoaiCauHoi,
                    ThuTu = qOrder++,
                    BatBuoc = true
                };
                _context.CauHoiKhaoSat.Add(cauHoi);
                await _context.SaveChangesAsync();

                if (q.LoaiCauHoi == "Một lựa chọn" || q.LoaiCauHoi == "Nhiều lựa chọn")
                {
                    int optOrder = 1;
                    foreach (var opt in q.Options!)
                    {
                        _context.LuaChonCauHoi.Add(new LuaChonCauHoi
                        {
                            MaCauHoi = cauHoi.MaCauHoi,
                            NoiDung = opt,
                            ThuTu = optOrder++
                        });
                    }

                    await _context.SaveChangesAsync();
                }
            }

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

            return Json(new { success = true, message = $"Đã lưu khảo sát và gửi phiếu đến {activeCustomers.Count} khách hàng thành công!" });
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

            ph.MaQL = quanLy.MaQL;
            ph.PhanHoiQuanLy = PhanHoiQuanLy.Trim();
            ph.TrangThai = "Đã xử lý";
            ph.NgayXuLy = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã gửi phúc đáp đến khách hàng thành công!";
            return RedirectToAction(nameof(SurveyFeedback));
        }

        // ==============================================================================
        // 5. TRANG 3: BÁO CÁO THỐNG KÊ CRM TÍNH TOÁN 100% TỪ CSDL
        // ==============================================================================
        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            // A. Đếm tổng số khách hàng
            int totalCustomers = await _context.KhachHang.CountAsync(k => !k.DaXoa);
            ViewBag.TotalCustomers = totalCustomers;

            // B. Tính tỷ lệ nhóm tuổi
            var customerBirthDates = await _context.KhachHang
                .Where(k => !k.DaXoa && k.NgaySinh.HasValue && k.NgaySinh.Value <= DateTime.Today)
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
                        COUNT(DISTINCT CASE WHEN kh.DaXoa = 0 THEN ks.MaKH END) AS SoLuong
                    FROM DANHMUC dm
                    LEFT JOIN KHACHHANG_SOTHICH ks ON dm.MaDM = ks.MaDM
                    LEFT JOIN KHACHHANG kh ON kh.MaKH = ks.MaKH
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

            // D. Danh sách bài khảo sát cho Dropdown
            ViewBag.SurveysList = await _context.KhaoSat.OrderByDescending(k => k.NgayBatDau).ToListAsync();

            return View("~/Views/Admin/Survey/Analytics.cshtml");
        }
            // ==============================================================================
// 6. API THỐNG KÊ KẾT QUẢ CHO TỪNG BÀI KHẢO SÁT MẪU ÁO MỚI
// ==============================================================================
        [HttpGet]
        public async Task<IActionResult> GetSurveyChartData(int maKS)
        {
            var survey = await _context.KhaoSat
                .AsNoTracking()
                .Include(k => k.CauHoiKhaoSats.OrderBy(q => q.ThuTu))
                    .ThenInclude(q => q.LuaChonCauHois.OrderBy(option => option.ThuTu))
                .FirstOrDefaultAsync(k => k.MaKS == maKS);

            if (survey == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài khảo sát." });
            }

            var questionResults = new List<object>();
            foreach (var question in survey.CauHoiKhaoSats.OrderBy(q => q.ThuTu))
            {
                var answers = await _context.TraLoiKhaoSat
                    .AsNoTracking()
                    .Where(answer => answer.MaCauHoi == question.MaCauHoi
                        && answer.PhieuKhaoSat != null
                        && answer.PhieuKhaoSat.MaKS == maKS
                        && answer.PhieuKhaoSat.TrangThai == "Đã hoàn thành")
                    .Select(answer => new
                    {
                        answer.MaPhieuKS,
                        answer.MaLuaChon,
                        answer.SoSao
                    })
                    .ToListAsync();

                string[] labels;
                int[] counts;
                int responseCount;
                bool chartable;

                if (question.LoaiCauHoi == "Một lựa chọn" || question.LoaiCauHoi == "Nhiều lựa chọn")
                {
                    var options = question.LuaChonCauHois.OrderBy(option => option.ThuTu).ToList();
                    labels = options.Select(option => option.NoiDung).ToArray();
                    counts = options.Select(option => answers.Count(answer => answer.MaLuaChon == option.MaLuaChon)).ToArray();
                    responseCount = answers.Where(answer => answer.MaLuaChon.HasValue)
                        .Select(answer => answer.MaPhieuKS).Distinct().Count();
                    chartable = options.Count > 0;
                }
                else if (question.LoaiCauHoi == "Đánh giá sao")
                {
                    labels = new[] { "1 sao", "2 sao", "3 sao", "4 sao", "5 sao" };
                    counts = Enumerable.Range(1, 5)
                        .Select(star => answers.Count(answer => answer.SoSao == star))
                        .ToArray();
                    responseCount = answers.Where(answer => answer.SoSao.HasValue)
                        .Select(answer => answer.MaPhieuKS).Distinct().Count();
                    chartable = true;
                }
                else
                {
                    labels = Array.Empty<string>();
                    counts = Array.Empty<int>();
                    responseCount = answers.Count;
                    chartable = false;
                }

                var percents = counts.Select(count => responseCount == 0
                    ? 0
                    : Math.Round((double)count * 100.0 / responseCount, 1)).ToArray();

                questionResults.Add(new
                {
                    maCauHoi = question.MaCauHoi,
                    question = question.NoiDung,
                    questionType = question.LoaiCauHoi,
                    chartable,
                    labels,
                    counts,
                    percents,
                    responseCount
                });
            }

            return Json(new { success = true, survey = survey.TenKhaoSat, questions = questionResults });
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

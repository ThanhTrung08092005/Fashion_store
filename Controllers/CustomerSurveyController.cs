using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;

namespace Fashion_store.Controllers
{
    public class CustomerSurveyController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public CustomerSurveyController(FashionStoreDbContext context)
        {
            _context = context;
        }


        // =========================================================================
        // HÀM BẢO MẬT: Kiểm tra tài khoản có đúng MaVaiTro == 2 hay không
        // =========================================================================
        private async Task<(bool IsValid, int MaKH, string ErrorMessage)> CheckCustomerRoleAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return (false, 0, "Vui lòng đăng nhập bằng tài khoản Khách hàng để tiếp tục.");
            }

            var accountIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdValue, out int maTK))
            {
                return (false, 0, "Không xác định được tài khoản đang đăng nhập.");
            }

            var taiKhoan = await _context.TaiKhoan
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.MaTK == maTK);

            if (taiKhoan == null || !taiKhoan.TrangThai)
            {
                return (false, 0, "Tài khoản không tồn tại hoặc đã bị khóa.");
            }

            if (taiKhoan.MaVaiTro != 2)
            {
                return (false, 0, "Khu vực khách hàng chỉ dành cho tài khoản Khách hàng.");
            }

            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(k => k.MaTK == maTK && !k.DaXoa);

            if (khachHang == null)
            {
                return (false, 0, "Không tìm thấy hồ sơ khách hàng đang hoạt động.");
            }

            // Đồng bộ Session phục vụ banner khảo sát; mã KH vẫn được tra từ tài khoản đã xác thực.
            HttpContext.Session.SetInt32("MaTK", maTK);
            HttpContext.Session.SetInt32("MaVaiTro", taiKhoan.MaVaiTro);
            HttpContext.Session.SetInt32("MaKH", khachHang.MaKH);

            return (true, khachHang.MaKH, string.Empty);
        }
        // =========================================================================
        // API KIỂM TRA PHIẾU KHẢO SÁT CHƯA LÀM (Dùng cho Banner thông báo)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> CheckPendingSurvey()
        {
            // Kiểm tra nghiêm ngặt MaVaiTro == 2 (Khách hàng)
            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                return Json(new { hasSurvey = false });
            }

            int currentCustomerId = check.MaKH;

            // Tìm bài khảo sát đang mở và chưa làm (TrangThai == 'Đang làm')
            var pendingTicket = await _context.PhieuKhaoSat
                .Include(p => p.KhaoSat)
                .Where(p => p.MaKH == currentCustomerId && 
                            p.TrangThai == "Đang làm" && 
                            p.KhaoSat != null && 
                            p.KhaoSat.TrangThai &&
                            p.KhaoSat.NgayBatDau <= DateTime.Today &&
                            p.KhaoSat.NgayKetThuc >= DateTime.Today)
                .OrderByDescending(p => p.NgayBatDau)
                .FirstOrDefaultAsync();

            if (pendingTicket == null)
            {
                return Json(new { hasSurvey = false });
            }

            return Json(new
            {
                hasSurvey = true,
                maPhieuKS = pendingTicket.MaPhieuKS,
                tenKhaoSat = pendingTicket.KhaoSat?.TenKhaoSat,
                moTa = pendingTicket.KhaoSat?.MoTa
            });
        }

        // =========================================================================
        // FORM GỬI PHẢN HỒI SẢN PHẨM TỪ TÀI KHOẢN KHÁCH HÀNG ĐANG ĐĂNG NHẬP
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                TempData["ErrorMessage"] = check.ErrorMessage;
                return RedirectToAction("Login", "Account", new
                {
                    area = "",
                    returnUrl = Url.Action(nameof(Feedback), "CustomerSurvey")
                });
            }

            await LoadActiveProductsAsync();
            return View(new CustomerFeedbackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(CustomerFeedbackViewModel model)
        {
            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                TempData["ErrorMessage"] = check.ErrorMessage;
                return RedirectToAction("Login", "Account", new
                {
                    area = "",
                    returnUrl = Url.Action(nameof(Feedback), "CustomerSurvey")
                });
            }

            var allowedFeedbackTypes = new[]
            {
                "Chất lượng sản phẩm",
                "Dịch vụ/đơn hàng",
                "Góp ý mẫu mới",
                "Khác"
            };

            if (!allowedFeedbackTypes.Contains(model.LoaiPhanHoi, StringComparer.Ordinal))
            {
                ModelState.AddModelError(nameof(model.LoaiPhanHoi), "Loại phản hồi không hợp lệ.");
            }

            if (model.MaSP.HasValue && !await _context.SanPham
                    .AnyAsync(s => s.MaSP == model.MaSP.Value && s.TrangThai))
            {
                ModelState.AddModelError(nameof(model.MaSP), "Sản phẩm đã chọn không tồn tại hoặc không còn kinh doanh.");
            }

            if (!ModelState.IsValid)
            {
                await LoadActiveProductsAsync();
                return View(model);
            }

            _context.PhanHoi.Add(new PhanHoi
            {
                // Không nhận MaKH từ form/request; luôn gắn với hồ sơ tra từ claim đăng nhập.
                MaKH = check.MaKH,
                MaSP = model.MaSP,
                TieuDe = string.IsNullOrWhiteSpace(model.TieuDe) ? null : model.TieuDe.Trim(),
                NoiDung = model.NoiDung.Trim(),
                LoaiPhanHoi = model.LoaiPhanHoi,
                SoSao = model.SoSao,
                TrangThai = "Chưa xử lý",
                NgayGui = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Phản hồi đã được gửi. Cảm ơn bạn đã đóng góp ý kiến!";
            return RedirectToAction(nameof(Feedback));
        }

        private async Task LoadActiveProductsAsync()
        {
            ViewBag.Products = await _context.SanPham
                .AsNoTracking()
                .Where(s => s.TrangThai)
                .OrderBy(s => s.TenSP)
                .ToListAsync();
        }

        // =========================================================================
        // 1. DANH SÁCH PHIẾU KHẢO SÁT (INDEX)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                TempData["ErrorMessage"] = check.ErrorMessage;
                return RedirectToAction("Login", "Account");
            }

            int currentCustomerId = check.MaKH;

            var allTickets = await _context.PhieuKhaoSat
                .Include(p => p.KhaoSat)
                    .ThenInclude(k => k!.CauHoiKhaoSats)
                .Where(p => p.MaKH == currentCustomerId)
                .OrderByDescending(p => p.NgayBatDau)
                .ToListAsync();

            var viewModel = new CustomerSurveyListViewModel
            {
                PhieuDangLam = allTickets
                    .Where(p => p.TrangThai == "Đang làm" && 
                               p.KhaoSat != null && p.KhaoSat.TrangThai &&
                               p.KhaoSat.NgayBatDau <= DateTime.Today &&
                               p.KhaoSat.NgayKetThuc >= DateTime.Today)
                    .ToList(),
                PhieuDaHoanThanh = allTickets
                    .Where(p => p.TrangThai == "Đã hoàn thành")
                    .ToList()
            };

            return View(viewModel);
        }

        // =========================================================================
        // 2. MÀN HÌNH LÀM BÀI KHẢO SÁT (DO SURVEY)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> DoSurvey(int id)
        {
            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                TempData["ErrorMessage"] = check.ErrorMessage;
                return RedirectToAction("Login", "Account");
            }

            int currentCustomerId = check.MaKH;

            var phieu = await _context.PhieuKhaoSat
                .Include(p => p.KhaoSat)
                    .ThenInclude(k => k!.CauHoiKhaoSats.OrderBy(c => c.ThuTu))
                        .ThenInclude(c => c.LuaChonCauHois.OrderBy(l => l.ThuTu))
                .FirstOrDefaultAsync(p => p.MaPhieuKS == id && p.MaKH == currentCustomerId);

            if (phieu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu khảo sát này hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

            if (phieu.TrangThai == "Đã hoàn thành")
            {
                TempData["InfoMessage"] = "Bạn đã hoàn thành bài khảo sát này trước đó rồi.";
                return RedirectToAction(nameof(Index));
            }

            if (phieu.KhaoSat == null || !phieu.KhaoSat.TrangThai
                || phieu.KhaoSat.NgayBatDau.Date > DateTime.Today
                || phieu.KhaoSat.NgayKetThuc.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Bài khảo sát hiện chưa mở hoặc đã hết hạn.";
                return RedirectToAction(nameof(Index));
            }

            return View(phieu);
        }

        // =========================================================================
        // 3. XỬ LÝ NỘP BÀI KHẢO SÁT (SUBMIT SURVEY)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSurvey([FromBody] SubmitSurveyDto model)
        {
            if (model == null || model.MaPhieuKS <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu khảo sát không hợp lệ." });
            }

            var check = await CheckCustomerRoleAsync();
            if (!check.IsValid)
            {
                return Json(new { success = false, message = check.ErrorMessage });
            }

            int currentCustomerId = check.MaKH;

            var phieu = await _context.PhieuKhaoSat
                .Include(p => p.KhaoSat)
                    .ThenInclude(k => k!.CauHoiKhaoSats)
                        .ThenInclude(question => question.LuaChonCauHois)
                .FirstOrDefaultAsync(p => p.MaPhieuKS == model.MaPhieuKS && p.MaKH == currentCustomerId);

            if (phieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin phiếu khảo sát." });
            }

            if (phieu.TrangThai == "Đã hoàn thành")
            {
                return Json(new { success = false, message = "Phiếu khảo sát này đã được nộp trước đó." });
            }

            if (phieu.KhaoSat == null || !phieu.KhaoSat.TrangThai
                || phieu.KhaoSat.NgayBatDau.Date > DateTime.Today
                || phieu.KhaoSat.NgayKetThuc.Date < DateTime.Today)
            {
                return Json(new { success = false, message = "Bài khảo sát hiện chưa mở hoặc đã hết hạn." });
            }

            var questions = phieu.KhaoSat.CauHoiKhaoSats.ToList();
            var submittedAnswers = model.Answers ?? new List<AnswerItemDto>();
            if (submittedAnswers.GroupBy(answer => answer.MaCauHoi).Any(group => group.Count() > 1)
                || submittedAnswers.Any(answer => questions.All(question => question.MaCauHoi != answer.MaCauHoi)))
            {
                return Json(new { success = false, message = "Câu trả lời chứa câu hỏi không hợp lệ." });
            }

            var answerByQuestion = submittedAnswers.ToDictionary(answer => answer.MaCauHoi);
            var traLoiSet = _context.Set<TraLoiKhaoSat>();

            foreach (var question in questions)
            {
                answerByQuestion.TryGetValue(question.MaCauHoi, out var answer);

                if (question.LoaiCauHoi == "Một lựa chọn")
                {
                    if (answer?.MaLuaChon == null)
                    {
                        if (question.BatBuoc)
                            return Json(new { success = false, message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }

                    var selectedOption = question.LuaChonCauHois
                        .FirstOrDefault(option => option.MaLuaChon == answer!.MaLuaChon!.Value);
                    if (selectedOption == null)
                    {
                        return Json(new { success = false, message = "Một phương án trả lời không thuộc câu hỏi đã chọn." });
                    }

                    traLoiSet.Add(new TraLoiKhaoSat
                    {
                        MaPhieuKS = phieu.MaPhieuKS,
                        MaCauHoi = question.MaCauHoi,
                        MaLuaChon = selectedOption.MaLuaChon
                    });
                }
                else if (question.LoaiCauHoi == "Nhiều lựa chọn")
                {
                    var selectedIds = answer?.DanhSachMaLuaChon ?? new List<int>();
                    var allowedIds = question.LuaChonCauHois.Select(option => option.MaLuaChon).ToHashSet();
                    if (selectedIds.Distinct().Count() != selectedIds.Count || selectedIds.Any(id => !allowedIds.Contains(id)))
                    {
                        return Json(new { success = false, message = "Một hoặc nhiều phương án trả lời không hợp lệ." });
                    }
                    if (question.BatBuoc && selectedIds.Count == 0)
                    {
                        return Json(new { success = false, message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                    }

                    foreach (var optionId in selectedIds)
                    {
                        traLoiSet.Add(new TraLoiKhaoSat
                        {
                            MaPhieuKS = phieu.MaPhieuKS,
                            MaCauHoi = question.MaCauHoi,
                            MaLuaChon = optionId
                        });
                    }
                }
                else if (question.LoaiCauHoi == "Đánh giá sao")
                {
                    if (answer?.SoSao == null)
                    {
                        if (question.BatBuoc)
                            return Json(new { success = false, message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }
                    int rating = answer!.SoSao!.Value;
                    if (rating < 1 || rating > 5)
                    {
                        return Json(new { success = false, message = "Số sao phải nằm trong khoảng từ 1 đến 5." });
                    }

                    traLoiSet.Add(new TraLoiKhaoSat
                    {
                        MaPhieuKS = phieu.MaPhieuKS,
                        MaCauHoi = question.MaCauHoi,
                        SoSao = rating
                    });
                }
                else if (question.LoaiCauHoi == "Tự luận")
                {
                    var text = answer?.NoiDungTraLoi?.Trim();
                    if (string.IsNullOrEmpty(text))
                    {
                        if (question.BatBuoc)
                            return Json(new { success = false, message = "Vui lòng trả lời đầy đủ các câu hỏi bắt buộc." });
                        continue;
                    }
                    if (text.Length > 4000)
                    {
                        return Json(new { success = false, message = "Câu trả lời tự luận không được vượt quá 4.000 ký tự." });
                    }

                    traLoiSet.Add(new TraLoiKhaoSat
                    {
                        MaPhieuKS = phieu.MaPhieuKS,
                        MaCauHoi = question.MaCauHoi,
                        NoiDungTraLoi = text
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Bài khảo sát chứa loại câu hỏi không được hỗ trợ." });
                }
            }

            phieu.TrangThai = "Đã hoàn thành";
            phieu.NgayNop = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Cảm ơn bạn đã tham gia đóng góp ý kiến về mẫu áo mới!" 
            });
        }
    }
}

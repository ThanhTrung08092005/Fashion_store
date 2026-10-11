using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using Fashion_store.ViewModels;

namespace Fashion_store.Controllers
{
    public class AccountController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public AccountController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // ĐĂNG NHẬP KHÁCH HÀNG (CUSTOMER STOREFRONT)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, bool locked = false)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.HasClaim("MaVaiTro", "2")) return RedirectToAction("Index", "Home");
                return RedirectToAction("AccountManagement", "Account", new { area = "Admin" });
            }
            if (locked)
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.");
            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/Account/Login.cshtml", new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View("~/Views/Account/Login.cshtml", model);
            }

            string inputUsername = model.TenDangNhap?.Trim() ?? string.Empty;
            string inputPassword = model.MatKhau ?? string.Empty;
            if (inputUsername.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

            // Tìm tài khoản Khách hàng theo Tên đăng nhập hoặc Email
            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.VaiTro)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.TenDangNhap == inputUsername 
                    || (t.KhachHang != null && t.KhachHang.Email == inputUsername));

            if (taiKhoan == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email không tồn tại trong hệ thống.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

            if (!PasswordHelper.VerifyAndUpgrade(taiKhoan, inputPassword))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác. Vui lòng kiểm tra lại.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

            if (taiKhoan.MaVaiTro != 2 || taiKhoan.KhachHang == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

            if (!taiKhoan.TrangThai || taiKhoan.KhachHang.DaXoa)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }
            await _context.SaveChangesAsync();

            string hoTen = taiKhoan.KhachHang?.HoTen ?? taiKhoan.TenDangNhap;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, taiKhoan.VaiTro?.TenVaiTro ?? "Khách hàng"),
                new Claim("HoTen", hoTen),
                new Claim("MaVaiTro", taiKhoan.MaVaiTro.ToString()),
                new Claim("UserType", "KHACHHANG")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            bool hasPreferences = await _context.KhachHangSoThich.AnyAsync(x => x.MaKH == taiKhoan.KhachHang!.MaKH);
            if (!hasPreferences)
                return RedirectToAction(nameof(Preferences));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Chuyển về Trang chủ Khách hàng
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = "Khách hàng")]
        public async Task<IActionResult> Preferences()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountId, out var maTK)) return Challenge();
            var khachHang = await _context.KhachHang
                .Include(k => k.SoThichList)
                .FirstOrDefaultAsync(k => k.MaTK == maTK && !k.DaXoa);
            if (khachHang == null) return Forbid();

            ViewBag.Categories = await _context.DanhMuc.Where(d => d.TrangThai).OrderBy(d => d.MaDM).ToListAsync();
            ViewBag.SelectedIds = khachHang.SoThichList.Select(s => s.MaDM).ToHashSet();
            return View("~/Views/Account/Preferences.cshtml");
        }

        [HttpPost]
        [Authorize(Roles = "Khách hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preferences(List<int> selectedCategoryIds)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountId, out var maTK)) return Challenge();
            var khachHang = await _context.KhachHang
                .Include(k => k.SoThichList)
                .FirstOrDefaultAsync(k => k.MaTK == maTK && !k.DaXoa);
            if (khachHang == null) return Forbid();

            var ids = selectedCategoryIds.Distinct().ToList();
            var categories = await _context.DanhMuc
                .Where(d => ids.Contains(d.MaDM) && d.TrangThai)
                .ToListAsync();
            if (ids.Count == 0 || ids.Count > 5 || categories.Count != ids.Count)
            {
                ModelState.AddModelError(string.Empty, "Hãy chọn từ 1 đến 5 sở thích hợp lệ.");
                ViewBag.Categories = await _context.DanhMuc.Where(d => d.TrangThai).OrderBy(d => d.MaDM).ToListAsync();
                ViewBag.SelectedIds = ids.ToHashSet();
                return View("~/Views/Account/Preferences.cshtml");
            }

            _context.KhachHangSoThich.RemoveRange(khachHang.SoThichList);
            _context.KhachHangSoThich.AddRange(categories.Select(d => new KhachHangSoThich
            {
                MaKH = khachHang.MaKH,
                MaDM = d.MaDM
            }));
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu sở thích của bạn.";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // ĐĂNG KÝ TÀI KHOẢN KHÁCH HÀNG (CRM 4.3)
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCustomer(RegisterCustomerViewModel model)
        {
            if (!ModelState.IsValid || model.TenDangNhap.Any(char.IsWhiteSpace))
            {
                TempData["ErrorMessage"] = "Thông tin đăng ký chưa hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Account/Login.cshtml");
            }

            bool existsUsername = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (existsUsername)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã được sử dụng.";
                return View("~/Views/Account/Login.cshtml");
            }

            bool existsEmail = await _context.KhachHang.AnyAsync(k => k.Email == model.Email);
            if (existsEmail)
            {
                TempData["ErrorMessage"] = $"Email '{model.Email}' đã được đăng ký.";
                return View("~/Views/Account/Login.cshtml");
            }

            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = PasswordHelper.Hash(model.MatKhau),
                MaVaiTro = vaiTroKH?.MaVaiTro ?? 2,
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            var khachHang = new KhachHang
            {
                MaTK = taiKhoan.MaTK,
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                NgayDangKy = DateTime.Now,
                DaXoa = false
            };

            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản khách hàng thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login), new { area = "", loggedOut = true });
        }
    }
}

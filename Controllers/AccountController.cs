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
        public IActionResult Login(string? returnUrl = null)
        {
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

            if (taiKhoan.MatKhau != inputPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác. Vui lòng kiểm tra lại.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

            if (!taiKhoan.TrangThai)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị KHÓA. Vui lòng liên hệ hỗ trợ.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Views/Account/Login.cshtml", model);
            }

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

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Chuyển về Trang chủ Khách hàng
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
            if (!ModelState.IsValid)
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
                MatKhau = model.MatKhau,
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}

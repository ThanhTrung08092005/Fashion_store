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

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public AccountController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP HỆ THỐNG (LOGIN)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Areas/Admin/Views/Account/Login.cshtml", new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            string inputUsername = model.TenDangNhap?.Trim() ?? string.Empty;
            string inputPassword = model.MatKhau ?? string.Empty;

            // Tìm tài khoản theo Tên đăng nhập hoặc Email (Quản lý hoặc Khách hàng)
            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.VaiTro)
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.TenDangNhap == inputUsername 
                    || (t.QuanLy != null && t.QuanLy.Email == inputUsername)
                    || (t.KhachHang != null && t.KhachHang.Email == inputUsername));

            if (taiKhoan == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email không tồn tại trong hệ thống.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            if (taiKhoan.MatKhau != inputPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác. Vui lòng kiểm tra lại.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            // Kiểm tra trạng thái tài khoản
            if (!taiKhoan.TrangThai)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị KHÓA. Vui lòng liên hệ Quản trị viên.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            // Lấy Tên hiển thị
            string hoTen = taiKhoan.QuanLy?.HoTen ?? taiKhoan.KhachHang?.HoTen ?? taiKhoan.TenDangNhap;

            // Tạo Claims phiên làm việc
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, taiKhoan.VaiTro?.TenVaiTro ?? "Khách hàng"),
                new Claim("HoTen", hoTen),
                new Claim("MaVaiTro", taiKhoan.MaVaiTro.ToString()),
                new Claim("UserType", taiKhoan.QuanLy != null ? "QUANLY" : "KHACHHANG")
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

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 2. KHÁCH HÀNG ĐĂNG KÝ TÀI KHOẢN MỚI (CRM 4.3)
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCustomer(RegisterCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin đăng ký chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Login));
            }

            // Kiểm tra trùng Tên đăng nhập hoặc Email
            bool existsUsername = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (existsUsername)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã được sử dụng.";
                return RedirectToAction(nameof(Login));
            }

            bool existsEmail = await _context.KhachHang.AnyAsync(k => k.Email == model.Email);
            if (existsEmail)
            {
                TempData["ErrorMessage"] = $"Email '{model.Email}' đã được đăng ký tài khoản khách hàng.";
                return RedirectToAction(nameof(Login));
            }

            // Lấy MaVaiTro cho Khách hàng (Mặc định = 2 hoặc tìm theo tên)
            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);
            int maVaiTro = vaiTroKH?.MaVaiTro ?? 2;

            // Tạo TaiKhoan
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau,
                MaVaiTro = maVaiTro,
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            // Tạo Hồ Sơ Khách Hàng (CRM)
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

            TempData["SuccessMessage"] = "Đăng ký tài khoản khách hàng thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ==========================================

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // 4. QUẢN LÝ TÀI KHOẢN & CRM (ACCOUNT & CUSTOMER MANAGEMENT)
        // ==========================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, string? chucVu, int? roleId, bool? status, string accountType = "KHACHHANG")
        {
            if (string.IsNullOrEmpty(accountType) || (accountType != "QUANLY" && accountType != "KHACHHANG"))
            {
                accountType = "KHACHHANG";
            }

            ViewBag.Roles = await _context.VaiTro.ToListAsync();
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentChucVu = chucVu;
            ViewBag.CurrentRoleId = roleId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentAccountType = accountType;

            // Lấy danh sách tài khoản - ẨN tài khoản Quản trị viên hệ thống (admin)
            var query = _context.TaiKhoan
                .Include(t => t.VaiTro)
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                .Where(t => t.TenDangNhap != "admin" && t.MaTK != 1)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string s = searchString.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(s)
                    || (t.QuanLy != null && (t.QuanLy.HoTen.ToLower().Contains(s) || (t.QuanLy.Email != null && t.QuanLy.Email.ToLower().Contains(s)) || (t.QuanLy.SoDienThoai != null && t.QuanLy.SoDienThoai.Contains(s))))
                    || (t.KhachHang != null && (t.KhachHang.HoTen.ToLower().Contains(s) || t.KhachHang.Email.ToLower().Contains(s) || (t.KhachHang.SoDienThoai != null && t.KhachHang.SoDienThoai.Contains(s)))));
            }

            // Lọc theo Chức vụ nhân sự (khi ở tab Nhân sự)
            if (!string.IsNullOrWhiteSpace(chucVu) && accountType == "QUANLY")
            {
                query = query.Where(t => t.QuanLy != null && t.QuanLy.ChucVu == chucVu);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.TrangThai == status.Value);
            }

            if (accountType == "QUANLY")
            {
                query = query.Where(t => t.QuanLy != null);
            }
            else
            {
                query = query.Where(t => t.KhachHang != null && !t.KhachHang.DaXoa);
            }

            var result = await query
                .OrderByDescending(t => t.NgayTao)
                .Select(t => new AccountItemViewModel
                {
                    MaTK = t.MaTK,
                    TenDangNhap = t.TenDangNhap,
                    MaVaiTro = t.MaVaiTro,
                    TenVaiTro = t.VaiTro != null ? t.VaiTro.TenVaiTro : "Chưa phân quyền",
                    TrangThai = t.TrangThai,
                    NgayTao = t.NgayTao,
                    UserType = t.QuanLy != null ? "QUANLY" : "KHACHHANG",

                    // Info QuanLy
                    MaQL = t.QuanLy != null ? t.QuanLy.MaQL : null,
                    ChucVu = t.QuanLy != null ? t.QuanLy.ChucVu : null,

                    // Info KhachHang (CRM)
                    MaKH = t.KhachHang != null ? t.KhachHang.MaKH : null,
                    HoTen = t.QuanLy != null ? t.QuanLy.HoTen : (t.KhachHang != null ? t.KhachHang.HoTen : t.TenDangNhap),
                    Email = t.QuanLy != null ? t.QuanLy.Email : (t.KhachHang != null ? t.KhachHang.Email : null),
                    SoDienThoai = t.QuanLy != null ? t.QuanLy.SoDienThoai : (t.KhachHang != null ? t.KhachHang.SoDienThoai : null),
                    NgaySinh = t.KhachHang != null ? t.KhachHang.NgaySinh : null,
                    GioiTinh = t.KhachHang != null ? t.KhachHang.GioiTinh : null,
                    DiaChi = t.KhachHang != null ? t.KhachHang.DiaChi : null,
                    SoThich = t.KhachHang != null ? t.KhachHang.SoThich : null
                })
                .ToListAsync();

            return View("~/Areas/Admin/Views/Account/Index.cshtml", result);
        }

        // ==========================================
        // 5. THÊM TÀI KHOẢN KHÁCH HÀNG CRM MỚI (CRM 4.1)
        // ==========================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CreateCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin nhập vào chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (exists)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau,
                MaVaiTro = vaiTroKH?.MaVaiTro ?? 2,
                TrangThai = model.TrangThai,
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
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                DiaChi = model.DiaChi,
                SoThich = model.SoThich,
                NgayDangKy = DateTime.Now,
                DaXoa = false
            };

            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm khách hàng '{model.HoTen}' thành công!";
            return RedirectToAction(nameof(Index), new { accountType = "KHACHHANG" });
        }

        // ==========================================
        // 6. THÊM TÀI KHOẢN QUẢN LÝ / ADMIN MỚI
        // ==========================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin nhập vào chưa hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (exists)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau,
                MaVaiTro = model.MaVaiTro,
                TrangThai = model.TrangThai,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            var quanLy = new QuanLy
            {
                MaTK = taiKhoan.MaTK,
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                ChucVu = model.ChucVu ?? "Quản lý Cửa Hàng",
                TrangThai = model.TrangThai
            };

            _context.QuanLy.Add(quanLy);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tạo tài khoản quản lý '{model.TenDangNhap}' thành công!";
            return RedirectToAction(nameof(Index), new { accountType = "QUANLY" });
        }

        // ==========================================
        // 7. SỬA TÀI KHOẢN & PHÂN QUYỀN (EDIT & PERMISSION)
        // ==========================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(model.MaTK);
            if (taiKhoan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            if (taiKhoan.MaTK == 1 || taiKhoan.TenDangNhap == "admin")
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa tài khoản Quản trị viên hệ thống (Admin).";
                return RedirectToAction(nameof(Index));
            }

            // Đổi vai trò (Phân quyền) & Mật khẩu
            taiKhoan.MaVaiTro = model.MaVaiTro;
            taiKhoan.TrangThai = model.TrangThai;

            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                taiKhoan.MatKhau = model.MatKhauMoi;
            }

            // Cập nhật thông tin Quản lý nếu là Quản lý
            if (model.UserType == "QUANLY" || model.MaQL.HasValue)
            {
                var quanLy = await _context.QuanLy.FirstOrDefaultAsync(q => q.MaTK == model.MaTK);
                if (quanLy != null)
                {
                    quanLy.HoTen = model.HoTen;
                    quanLy.Email = model.Email;
                    quanLy.SoDienThoai = model.SoDienThoai;
                    quanLy.ChucVu = model.ChucVu;
                    quanLy.TrangThai = model.TrangThai;
                }
            }

            // Cập nhật thông tin Khách hàng nếu là Khách hàng CRM
            if (model.UserType == "KHACHHANG" || model.MaKH.HasValue)
            {
                var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.MaTK == model.MaTK);
                if (khachHang != null)
                {
                    khachHang.HoTen = model.HoTen;
                    if (!string.IsNullOrWhiteSpace(model.Email)) khachHang.Email = model.Email;
                    khachHang.SoDienThoai = model.SoDienThoai;
                    khachHang.NgaySinh = model.NgaySinh;
                    khachHang.GioiTinh = model.GioiTinh;
                    khachHang.DiaChi = model.DiaChi;
                    khachHang.SoThich = model.SoThich;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cập nhật phân quyền & thông tin thành công!";
            string targetTab = (model.UserType == "QUANLY" || model.MaQL.HasValue) ? "QUANLY" : "KHACHHANG";
            return RedirectToAction(nameof(Index), new { accountType = targetTab });
        }

        // ==========================================
        // 8. KHÓA / MỞ KHÓA TÀI KHOẢN (CRM 4.1)
        // ==========================================

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            if (taiKhoan.MaTK == 1 || taiKhoan.TenDangNhap == "admin")
            {
                return Json(new { success = false, message = "Không thể khóa tài khoản Quản trị viên hệ thống (Admin)." });
            }

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _context.SaveChangesAsync();

            string stt = taiKhoan.TrangThai ? "Kích hoạt" : "Khóa";
            return Json(new { success = true, newStatus = taiKhoan.TrangThai, message = $"Đã {stt} tài khoản '{taiKhoan.TenDangNhap}'" });
        }

        // ==========================================
        // 9. XÓA TÀI KHOẢN / KHÁCH HÀNG (CRM 4.1)
        // ==========================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTK == id);

            if (taiKhoan == null)
            {
                TempData["ErrorMessage"] = "Tài khoản không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (taiKhoan.MaTK == 1 || taiKhoan.TenDangNhap == "admin")
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Quản trị viên hệ thống (Admin).";
                return RedirectToAction(nameof(Index));
            }

            string targetTab = taiKhoan.QuanLy != null ? "QUANLY" : "KHACHHANG";

            if (taiKhoan.KhachHang != null)
            {
                // Xóa mềm cho Khách hàng CRM
                taiKhoan.KhachHang.DaXoa = true;
                taiKhoan.TrangThai = false;
            }

            if (taiKhoan.QuanLy != null)
            {
                _context.QuanLy.Remove(taiKhoan.QuanLy);
                _context.TaiKhoan.Remove(taiKhoan);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa tài khoản '{taiKhoan.TenDangNhap}' thành công.";
            return RedirectToAction(nameof(Index), new { accountType = targetTab });
        }
    }
}

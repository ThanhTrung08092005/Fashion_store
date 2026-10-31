using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using Fashion_store.Services;
using Fashion_store.ViewModels;

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Quản lý, Admin")]
    public class AccountController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public AccountController(FashionStoreDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Chọn trang đích phù hợp với chức vụ để tránh đăng nhập xong bị 403
        /// (ví dụ Nhân viên Kho không có quyền vào Báo cáo & Thống kê).
        /// </summary>
        private IActionResult RedirectToLandingPage(ClaimsPrincipal user)
        {
            if (Permissions.CanManageFeedback(user))
            {
                return RedirectToAction("Analytics", "Survey", new { area = "Admin" });
            }

            if (Permissions.CanViewCustomers(user))
            {
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (Permissions.CanViewProducts(user))
            {
                return RedirectToAction("ProductManagement", "Product", new { area = "Admin" });
            }

            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // 1. ĐĂNG NHẬP ADMIN SYSTEM (LOGIN)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, bool locked = false)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Quản lý") || User.IsInRole("Admin"))
                    return RedirectToLandingPage(User);

                var deniedModel = new LoginViewModel();
                ModelState.AddModelError(string.Empty, "Tài khoản khách hàng không thể đăng nhập vào trang quản trị.");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", deniedModel);
            }

            if (locked)
                ModelState.AddModelError(string.Empty, "Tài khoản quản lý của bạn đã bị khóa. Vui lòng liên hệ Admin.");
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
            if (inputUsername.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            // Tìm tài khoản theo Tên đăng nhập hoặc Email
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

            if (!PasswordHelper.VerifyAndUpgrade(taiKhoan, inputPassword))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác. Vui lòng kiểm tra lại.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            if (!taiKhoan.TrangThai || (taiKhoan.MaVaiTro == 1 && taiKhoan.QuanLy?.TrangThai != true))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị KHÓA. Vui lòng liên hệ Quản trị viên.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }
            await _context.SaveChangesAsync();

            if (taiKhoan.MaVaiTro is not (1 or 3))
            {
                ModelState.AddModelError(string.Empty, "Trang quản trị chỉ dành cho Admin và Quản lý.");
                model.MatKhau = string.Empty;
                ModelState.Remove("MatKhau");
                return View("~/Areas/Admin/Views/Account/Login.cshtml", model);
            }

            string hoTen = taiKhoan.QuanLy?.HoTen ?? taiKhoan.KhachHang?.HoTen ?? taiKhoan.TenDangNhap;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, taiKhoan.VaiTro?.TenVaiTro ?? "Khách hàng"),
                new Claim("HoTen", hoTen),
                new Claim("MaVaiTro", taiKhoan.MaVaiTro.ToString()),
                new Claim("ChucVu", Permissions.Normalize(taiKhoan.QuanLy?.ChucVu)),
                new Claim("UserType", taiKhoan.QuanLy != null ? "QUANLY" : "KHACHHANG")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userPrincipal,
                authProperties);
            HttpContext.Session.SetInt32("MaTK", taiKhoan.MaTK);
            HttpContext.Session.SetInt32("MaVaiTro", taiKhoan.MaVaiTro);
            if (taiKhoan.KhachHang != null)
            {
                HttpContext.Session.SetInt32("MaKH", taiKhoan.KhachHang.MaKH);
            }
            // =====================================================================

            return RedirectToLandingPage(userPrincipal);
        }

        private async Task ReleaseSoftDeletedAccountConflictsAsync(string? username, string? email)
        {
            string? normUser = string.IsNullOrWhiteSpace(username) ? null : username.Trim().ToLower();
            string? normEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();
            if (normUser == null && normEmail == null) return;

            var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            bool changed = false;

            if (normUser != null)
            {
                var conflictAccounts = await _context.TaiKhoan
                    .Include(t => t.KhachHang)
                    .Include(t => t.QuanLy)
                    .Where(t => t.TenDangNhap.ToLower() == normUser
                        && ((t.KhachHang != null && t.KhachHang.DaXoa)
                            || (t.MaVaiTro == 1 && !t.TrangThai && (t.QuanLy == null || !t.QuanLy.TrangThai))))
                    .ToListAsync();

                foreach (var acc in conflictAccounts)
                {
                    string archivedUser = $"del_{acc.MaTK}_{stamp}";
                    acc.TenDangNhap = archivedUser.Length > 50 ? archivedUser.Substring(0, 50) : archivedUser;
                    if (acc.KhachHang != null && acc.KhachHang.DaXoa && !acc.KhachHang.Email.StartsWith("del_"))
                    {
                        string archivedEmail = $"del_{acc.KhachHang.MaKH}_{stamp}@deleted.local";
                        acc.KhachHang.Email = archivedEmail.Length > 100 ? archivedEmail.Substring(0, 100) : archivedEmail;
                    }
                    changed = true;
                }
            }

            if (normEmail != null)
            {
                var conflictCustomers = await _context.KhachHang
                    .Include(k => k.TaiKhoan)
                    .Where(k => k.Email.ToLower() == normEmail && k.DaXoa)
                    .ToListAsync();

                foreach (var kh in conflictCustomers)
                {
                    string archivedEmail = $"del_{kh.MaKH}_{stamp}@deleted.local";
                    kh.Email = archivedEmail.Length > 100 ? archivedEmail.Substring(0, 100) : archivedEmail;
                    if (kh.TaiKhoan != null && !kh.TaiKhoan.TenDangNhap.StartsWith("del_"))
                    {
                        string archivedUser = $"del_{kh.TaiKhoan.MaTK}_{stamp}";
                        kh.TaiKhoan.TenDangNhap = archivedUser.Length > 50 ? archivedUser.Substring(0, 50) : archivedUser;
                    }
                    changed = true;
                }

                var conflictManagers = await _context.QuanLy
                    .Include(q => q.TaiKhoan)
                    .Where(q => q.Email != null && q.Email.ToLower() == normEmail
                        && !q.TrangThai && (q.TaiKhoan == null || !q.TaiKhoan.TrangThai || q.TaiKhoan.TenDangNhap.StartsWith("del_")))
                    .ToListAsync();

                foreach (var ql in conflictManagers)
                {
                    string archivedEmail = $"del_ql_{ql.MaQL}_{stamp}@deleted.local";
                    ql.Email = archivedEmail.Length > 100 ? archivedEmail.Substring(0, 100) : archivedEmail;
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
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

            await ReleaseSoftDeletedAccountConflictsAsync(model.TenDangNhap, model.Email);

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

            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);
            int maVaiTro = vaiTroKH?.MaVaiTro ?? 2;

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = PasswordHelper.Hash(model.MatKhau),
                MaVaiTro = maVaiTro,
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

            TempData["SuccessMessage"] = "Đăng ký tài khoản khách hàng thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login), new { area = "Admin", loggedOut = true });
        }

        // ==========================================
        // 4. QUẢN LÝ TÀI KHOẢN & KHÁCH HÀNG CRM (ACCOUNT MANAGEMENT)
        // ==========================================
        [HttpGet]
        [ActionName("AccountManagement")]
        [Authorize(Policy = Permissions.PolicyCustomersView)]
        public async Task<IActionResult> AccountManagement(string? searchString, string? chucVu, int? roleId, bool? status, string accountType = "KHACHHANG")
        {
            bool isAdmin = Permissions.IsAdmin(User);

            // Chỉ Admin mới được xem danh sách nhân sự & phân quyền.
            if (!isAdmin)
            {
                accountType = "KHACHHANG";
            }

            if (string.IsNullOrEmpty(accountType) || (accountType != "QUANLY" && accountType != "KHACHHANG"))
            {
                accountType = "KHACHHANG";
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.Roles = await _context.VaiTro.ToListAsync();
            var categories = await _context.DanhMuc
                .AsNoTracking()
                .OrderBy(category => category.TenDM)
                .ToListAsync();
            ViewBag.DanhMucList = categories;
            ViewBag.DanhMucs = categories;
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentChucVu = chucVu;
            ViewBag.CurrentRoleId = roleId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentAccountType = accountType;

            var query = _context.TaiKhoan
                .Include(t => t.VaiTro)
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                    .ThenInclude(k => k!.SoThichList)
                        .ThenInclude(st => st!.DanhMuc)
                .Where(t => t.MaVaiTro != 3)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string s = searchString.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(s)
                    || (t.QuanLy != null && (t.QuanLy.HoTen.ToLower().Contains(s) || (t.QuanLy.Email != null && t.QuanLy.Email.ToLower().Contains(s)) || (t.QuanLy.SoDienThoai != null && t.QuanLy.SoDienThoai.Contains(s))))
                    || (t.KhachHang != null && (t.KhachHang.HoTen.ToLower().Contains(s) || t.KhachHang.Email.ToLower().Contains(s) || (t.KhachHang.SoDienThoai != null && t.KhachHang.SoDienThoai.Contains(s)))));
            }



            if (status.HasValue)
            {
                query = query.Where(t => t.TrangThai == status.Value);
            }

            if (accountType == "QUANLY")
            {
                query = query.Where(t => t.MaVaiTro == 1 && t.QuanLy != null && !t.TenDangNhap.StartsWith("del_"));
            }
            else
            {
                query = query.Where(t => t.MaVaiTro == 2 && t.KhachHang != null && !t.KhachHang.DaXoa && !t.TenDangNhap.StartsWith("del_"));
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
                    UserType = t.MaVaiTro == 2 ? "KHACHHANG" : "QUANLY",

                    MaQL = t.QuanLy != null ? t.QuanLy.MaQL : null,
                    ChucVu = t.QuanLy != null ? t.QuanLy.ChucVu : null,

                    MaKH = t.KhachHang != null ? t.KhachHang.MaKH : null,
                    HoTen = t.MaVaiTro == 2 ? (t.KhachHang != null ? t.KhachHang.HoTen : t.TenDangNhap) : (t.QuanLy != null ? t.QuanLy.HoTen : t.TenDangNhap),
                    Email = t.MaVaiTro == 2 ? (t.KhachHang != null ? t.KhachHang.Email : null) : (t.QuanLy != null ? t.QuanLy.Email : null),
                    SoDienThoai = t.MaVaiTro == 2 ? (t.KhachHang != null ? t.KhachHang.SoDienThoai : null) : (t.QuanLy != null ? t.QuanLy.SoDienThoai : null),
                    NgaySinh = t.MaVaiTro == 2
                        ? (t.KhachHang != null ? t.KhachHang.NgaySinh : null)
                        : (t.QuanLy != null ? t.QuanLy.NgaySinh : null),
                    GioiTinh = t.MaVaiTro == 2
                        ? (t.KhachHang != null ? t.KhachHang.GioiTinh : null)
                        : (t.QuanLy != null ? t.QuanLy.GioiTinh : null),
                    DiaChi = t.KhachHang != null ? t.KhachHang.DiaChi : null,
                    SoThich = t.KhachHang != null && t.KhachHang.SoThichList.Any()
                        ? string.Join(", ", t.KhachHang.SoThichList.Select(st => st.DanhMuc != null ? st.DanhMuc.TenDM : ""))
                        : "Chưa chọn sở thích",
                    SoThichList = t.KhachHang != null && t.KhachHang.SoThichList.Any()
                        ? t.KhachHang.SoThichList.Where(st => st.DanhMuc != null).Select(st => st.DanhMuc!.TenDM).ToList()
                        : new List<string>(),
                    SoThichMaDMs = t.KhachHang != null && t.KhachHang.SoThichList.Any()
                        ? t.KhachHang.SoThichList.Select(st => st.MaDM).ToList()
                        : new List<int>()
                })
                .ToListAsync();

            // Lọc chức vụ sau khi đã chuẩn hoá để dữ liệu cũ ("Quản lý cửa hàng", "Quản lý của hàng"...)
            // vẫn khớp với 4 giá trị chuẩn trên giao diện.
            if (!string.IsNullOrWhiteSpace(chucVu) && accountType == "QUANLY")
            {
                var chucVuFilter = Permissions.Normalize(chucVu);
                result = result
                    .Where(item => Permissions.Normalize(item.ChucVu) == chucVuFilter)
                    .ToList();
            }

            return View("~/Areas/Admin/Views/Account/AccountManagement.cshtml", result);
        }

        // Tương thích route Index cũ (/Admin/Account hoặc /Admin/Account/Index) -> chuyển hướng đến AccountManagement
        [HttpGet]
        [Authorize(Policy = Permissions.PolicyCustomersView)]
        public IActionResult Index(string? searchString, string? chucVu, int? roleId, bool? status, string accountType = "KHACHHANG")
        {
            return RedirectToAction(nameof(AccountManagement), new { searchString, chucVu, roleId, status, accountType });
        }

        // ==========================================
        // 5. THÊM TÀI KHOẢN KHÁCH HÀNG CRM MỚI
        // ==========================================
        [Authorize(Policy = Permissions.PolicyCustomersManage)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CreateCustomerViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Headers.Accept.ToString().Contains("application/json");

            var username = model.TenDangNhap?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4 || username.Length > 50 || username.Any(char.IsWhiteSpace))
            {
                if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = "Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng." });
                TempData["ErrorMessage"] = "Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            await ReleaseSoftDeletedAccountConflictsAsync(username, model.Email);

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap.ToLower() == username.ToLower());
            if (exists)
            {
                if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = $"Tên đăng nhập '{username}' đã tồn tại." });
                TempData["ErrorMessage"] = $"Tên đăng nhập '{username}' đã tồn tại.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            var fullName = model.HoTen?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "HoTen", message = "Họ và tên không được để trống và tối đa 100 ký tự." });
                TempData["ErrorMessage"] = "Họ và tên không được để trống và tối đa 100 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            var email = model.Email?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = "Email không đúng định dạng." });
                TempData["ErrorMessage"] = "Email không đúng định dạng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            bool emailExists = await _context.KhachHang.AnyAsync(k => k.Email.ToLower() == email.ToLower())
                            || await _context.QuanLy.AnyAsync(q => q.Email.ToLower() == email.ToLower());
            if (emailExists)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = $"Email '{email}' đã được sử dụng bởi tài khoản khác." });
                TempData["ErrorMessage"] = $"Email '{email}' đã được sử dụng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            var phone = model.SoDienThoai?.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không được để trống." });
                TempData["ErrorMessage"] = "Số điện thoại không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (!phone.All(char.IsDigit) || phone.Length > 15)
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không đúng định dạng." });
                TempData["ErrorMessage"] = "Số điện thoại không đúng định dạng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (!model.NgaySinh.HasValue)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không được để trống." });
                TempData["ErrorMessage"] = "Ngày sinh không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (model.NgaySinh.Value.Date > DateTime.Today)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không thể nằm trong tương lai." });
                TempData["ErrorMessage"] = "Ngày sinh không thể nằm trong tương lai.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (string.IsNullOrWhiteSpace(model.GioiTinh))
            {
                if (isAjax) return Json(new { success = false, field = "GioiTinh", message = "Vui lòng chọn giới tính." });
                TempData["ErrorMessage"] = "Vui lòng chọn giới tính.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            var address = model.DiaChi?.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                if (isAjax) return Json(new { success = false, field = "DiaChi", message = "Địa chỉ không được để trống." });
                TempData["ErrorMessage"] = "Địa chỉ không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (address.Length > 255)
            {
                if (isAjax) return Json(new { success = false, field = "DiaChi", message = "Địa chỉ tối đa 255 ký tự." });
                TempData["ErrorMessage"] = "Địa chỉ tối đa 255 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            if (!string.IsNullOrWhiteSpace(model.MatKhau) && model.MatKhau.Trim().Length < 8)
            {
                if (isAjax) return Json(new { success = false, field = "MatKhau", message = "Mật khẩu tối thiểu 8 ký tự." });
                TempData["ErrorMessage"] = "Mật khẩu tối thiểu 8 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);

            string rawPassword = string.IsNullOrWhiteSpace(model.MatKhau) ? "12345678@" : model.MatKhau.Trim();

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = username,
                MatKhau = PasswordHelper.Hash(rawPassword),
                MaVaiTro = vaiTroKH?.MaVaiTro ?? 2,
                TrangThai = model.TrangThai,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            var khachHang = new KhachHang
            {
                MaTK = taiKhoan.MaTK,
                HoTen = fullName,
                Email = email,
                SoDienThoai = phone,
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                DiaChi = model.DiaChi,
                NgayDangKy = DateTime.Now,
                DaXoa = false
            };

            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            // Lưu danh sách sở thích ban đầu nếu có
            if (model.SoThichIds != null && model.SoThichIds.Any())
            {
                var selectedIds = model.SoThichIds.Distinct().ToList();
                var validCategories = await _context.DanhMuc
                    .Where(c => selectedIds.Contains(c.MaDM) && c.TrangThai)
                    .Select(c => c.MaDM)
                    .ToListAsync();

                _context.KhachHangSoThich.AddRange(validCategories.Select(dmId => new KhachHangSoThich
                {
                    MaKH = khachHang.MaKH,
                    MaDM = dmId
                }));
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Thêm khách hàng '{fullName}' thành công!";
            if (isAjax)
            {
                return Json(new { success = true, message = $"Thêm khách hàng '{fullName}' thành công!", redirectUrl = Url.Action(nameof(AccountManagement), new { accountType = "KHACHHANG" }) });
            }
            return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
        }

        // ==========================================
        // 6. THÊM TÀI KHOẢN QUẢN LÝ / ADMIN MỚI
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Headers.Accept.ToString().Contains("application/json");

            if (model.MaVaiTro != 1)
            {
                if (isAjax) return Json(new { success = false, message = "Admin chỉ có thể tạo tài khoản nhân viên Quản lý tại màn hình này." });
                TempData["ErrorMessage"] = "Admin chỉ có thể tạo tài khoản nhân viên Quản lý tại màn hình này.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            var username = model.TenDangNhap?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4 || username.Length > 50 || username.Any(char.IsWhiteSpace))
            {
                if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = "Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng." });
                TempData["ErrorMessage"] = "Tên đăng nhập phải có từ 4 đến 50 ký tự và không chứa khoảng trắng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            await ReleaseSoftDeletedAccountConflictsAsync(username, model.Email);

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap.ToLower() == username.ToLower());
            if (exists)
            {
                if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = $"Tên đăng nhập '{username}' đã tồn tại." });
                TempData["ErrorMessage"] = $"Tên đăng nhập '{username}' đã tồn tại.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            var fullName = model.HoTen?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "HoTen", message = "Họ và tên không được để trống và tối đa 100 ký tự." });
                TempData["ErrorMessage"] = "Họ và tên không được để trống và tối đa 100 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            var email = model.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = "Email không được để trống." });
                TempData["ErrorMessage"] = "Email không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = "Email không đúng định dạng hoặc vượt quá 100 ký tự." });
                TempData["ErrorMessage"] = "Email không đúng định dạng hoặc vượt quá 100 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            bool emailExists = await _context.KhachHang.AnyAsync(k => k.Email.ToLower() == email.ToLower())
                            || await _context.QuanLy.AnyAsync(q => q.Email.ToLower() == email.ToLower());
            if (emailExists)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = $"Email '{email}' đã được sử dụng bởi tài khoản khác." });
                TempData["ErrorMessage"] = $"Email '{email}' đã được sử dụng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            var phone = model.SoDienThoai?.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không được để trống." });
                TempData["ErrorMessage"] = "Số điện thoại không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (!phone.All(char.IsDigit) || phone.Length > 15)
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không đúng định dạng." });
                TempData["ErrorMessage"] = "Số điện thoại không đúng định dạng.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (!model.NgaySinh.HasValue)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không được để trống." });
                TempData["ErrorMessage"] = "Ngày sinh không được để trống.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (model.NgaySinh.Value.Date > DateTime.Today)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không thể nằm trong tương lai." });
                TempData["ErrorMessage"] = "Ngày sinh không thể nằm trong tương lai.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (string.IsNullOrWhiteSpace(model.GioiTinh))
            {
                if (isAjax) return Json(new { success = false, field = "GioiTinh", message = "Vui lòng chọn giới tính." });
                TempData["ErrorMessage"] = "Vui lòng chọn giới tính.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (string.IsNullOrWhiteSpace(model.ChucVu) || !Permissions.TryNormalize(model.ChucVu, out _))
            {
                if (isAjax) return Json(new { success = false, field = "ChucVu", message = "Vui lòng chọn chức vụ hợp lệ." });
                TempData["ErrorMessage"] = "Vui lòng chọn chức vụ hợp lệ.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            string chucVuChuan = Permissions.Normalize(model.ChucVu);
            string rawPassword = string.IsNullOrWhiteSpace(model.MatKhau) ? "123456" : model.MatKhau.Trim();

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = username,
                MatKhau = PasswordHelper.Hash(rawPassword),
                MaVaiTro = model.MaVaiTro,
                TrangThai = model.TrangThai,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            var quanLy = new QuanLy
            {
                MaTK = taiKhoan.MaTK,
                HoTen = fullName,
                Email = email,
                SoDienThoai = phone,
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                ChucVu = chucVuChuan,
                TrangThai = model.TrangThai
            };

            _context.QuanLy.Add(quanLy);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tạo tài khoản quản lý '{username}' thành công!";
            if (isAjax)
            {
                return Json(new { success = true, message = $"Tạo tài khoản quản lý '{username}' thành công!", redirectUrl = Url.Action(nameof(AccountManagement), new { accountType = "QUANLY" }) });
            }
            return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
        }

        // ==========================================
        // 7. SỬA TÀI KHOẢN & PHÂN QUYỀN (EDIT)
        // ==========================================
        [Authorize(Policy = Permissions.PolicyCustomersManage)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Headers.Accept.ToString().Contains("application/json");

            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTK == model.MaTK);
            if (taiKhoan == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy tài khoản cần chỉnh sửa." });
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool isAdmin = Permissions.IsAdmin(User);
            int previousRole = taiKhoan.MaVaiTro;
            if (taiKhoan.MaVaiTro == 3)
            {
                if (isAjax) return Json(new { success = false, message = "Không thể chỉnh sửa tài khoản Quản trị viên hệ thống (Admin)." });
                TempData["ErrorMessage"] = "Không thể chỉnh sửa tài khoản Quản trị viên hệ thống (Admin).";
                return RedirectToAction(nameof(AccountManagement));
            }

            // Quản lý (không phải Admin) chỉ được sửa hồ sơ khách hàng,
            // không được sửa tài khoản nhân sự khác và không được đổi vai trò.
            if (!isAdmin)
            {
                if (taiKhoan.MaVaiTro != 2) return Forbid();
                if (model.MaVaiTro != taiKhoan.MaVaiTro) return Forbid();
            }

            // Cập nhật Tên Đăng Nhập
            var newUsername = model.TenDangNhap?.Trim();
            await ReleaseSoftDeletedAccountConflictsAsync(newUsername, model.Email);
            if (!string.IsNullOrWhiteSpace(newUsername) && !string.Equals(newUsername, taiKhoan.TenDangNhap, StringComparison.OrdinalIgnoreCase))
            {
                if (newUsername.Length < 4 || newUsername.Length > 50)
                {
                    if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = "Tên đăng nhập phải có từ 4 đến 50 ký tự." });
                    TempData["ErrorMessage"] = "Tên đăng nhập phải có từ 4 đến 50 ký tự.";
                    return RedirectToAction(nameof(AccountManagement));
                }

                if (newUsername.Any(char.IsWhiteSpace))
                {
                    if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = "Tên đăng nhập không được chứa khoảng trắng." });
                    TempData["ErrorMessage"] = "Tên đăng nhập không được chứa khoảng trắng.";
                    return RedirectToAction(nameof(AccountManagement));
                }

                bool userExists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap.ToLower() == newUsername.ToLower() && t.MaTK != model.MaTK);
                if (userExists)
                {
                    if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = $"Tên đăng nhập '{newUsername}' đã tồn tại trong hệ thống." });
                    TempData["ErrorMessage"] = $"Tên đăng nhập '{newUsername}' đã tồn tại trong hệ thống.";
                    return RedirectToAction(nameof(AccountManagement));
                }

                taiKhoan.TenDangNhap = newUsername;
            }
            else if (string.IsNullOrWhiteSpace(newUsername))
            {
                if (isAjax) return Json(new { success = false, field = "TenDangNhap", message = "Tên đăng nhập không được để trống." });
                TempData["ErrorMessage"] = "Tên đăng nhập không được để trống.";
                return RedirectToAction(nameof(AccountManagement));
            }

            var fullName = model.HoTen?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "HoTen", message = "Họ và tên không được để trống và tối đa 100 ký tự." });
                TempData["ErrorMessage"] = "Họ và tên không được để trống và tối đa 100 ký tự.";
                return RedirectToAction(nameof(AccountManagement));
            }

            var email = model.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = "Email không được để trống." });
                TempData["ErrorMessage"] = "Email không được để trống.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 100)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = "Email không đúng định dạng." });
                TempData["ErrorMessage"] = "Email không đúng định dạng.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool emailExists = await _context.KhachHang.AnyAsync(k => k.Email.ToLower() == email.ToLower() && k.MaTK != model.MaTK)
                            || await _context.QuanLy.AnyAsync(q => q.Email.ToLower() == email.ToLower() && q.MaTK != model.MaTK);
            if (emailExists)
            {
                if (isAjax) return Json(new { success = false, field = "Email", message = $"Email '{email}' đã được sử dụng bởi tài khoản khác." });
                TempData["ErrorMessage"] = $"Email '{email}' đã được sử dụng bởi tài khoản khác.";
                return RedirectToAction(nameof(AccountManagement));
            }

            var phone = model.SoDienThoai?.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không được để trống." });
                TempData["ErrorMessage"] = "Số điện thoại không được để trống.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (!phone.All(char.IsDigit) || phone.Length > 15)
            {
                if (isAjax) return Json(new { success = false, field = "SoDienThoai", message = "Số điện thoại không đúng định dạng." });
                TempData["ErrorMessage"] = "Số điện thoại không đúng định dạng.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (!model.NgaySinh.HasValue)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không được để trống." });
                TempData["ErrorMessage"] = "Ngày sinh không được để trống.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (model.NgaySinh.Value.Date > DateTime.Today)
            {
                if (isAjax) return Json(new { success = false, field = "NgaySinh", message = "Ngày sinh không thể nằm trong tương lai." });
                TempData["ErrorMessage"] = "Ngày sinh không thể nằm trong tương lai.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (string.IsNullOrWhiteSpace(model.GioiTinh))
            {
                if (isAjax) return Json(new { success = false, field = "GioiTinh", message = "Vui lòng chọn giới tính." });
                TempData["ErrorMessage"] = "Vui lòng chọn giới tính.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool isCustomerAccount = taiKhoan.MaVaiTro == 2 || model.UserType == "KHACHHANG";
            if (isCustomerAccount)
            {
                var address = model.DiaChi?.Trim();
                if (string.IsNullOrWhiteSpace(address))
                {
                    if (isAjax) return Json(new { success = false, field = "DiaChi", message = "Địa chỉ không được để trống." });
                    TempData["ErrorMessage"] = "Địa chỉ không được để trống.";
                    return RedirectToAction(nameof(AccountManagement));
                }

                if (address.Length > 255)
                {
                    if (isAjax) return Json(new { success = false, field = "DiaChi", message = "Địa chỉ tối đa 255 ký tự." });
                    TempData["ErrorMessage"] = "Địa chỉ tối đa 255 ký tự.";
                    return RedirectToAction(nameof(AccountManagement));
                }
            }

            if (isCustomerAccount && !string.IsNullOrWhiteSpace(model.MatKhauMoi) && model.MatKhauMoi.Trim().Length < 8)
            {
                if (isAjax) return Json(new { success = false, field = "MatKhauMoi", message = "Mật khẩu mới tối thiểu 8 ký tự." });
                TempData["ErrorMessage"] = "Mật khẩu mới tối thiểu 8 ký tự.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
            }

            int resultingRole = isAdmin && model.MaVaiTro is 1 or 2
                ? model.MaVaiTro
                : taiKhoan.MaVaiTro;

            string? chucVuChuan = null;
            if (resultingRole == 1)
            {
                if (string.IsNullOrWhiteSpace(model.ChucVu) || !Permissions.TryNormalize(model.ChucVu, out _))
                {
                    if (isAjax) return Json(new { success = false, field = "ChucVu", message = "Vui lòng chọn chức vụ hợp lệ." });
                    TempData["ErrorMessage"] = "Vui lòng chọn chức vụ hợp lệ.";
                    return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
                }

                chucVuChuan = Permissions.Normalize(model.ChucVu);
            }

            if (isAdmin && model.MaVaiTro is 1 or 2)
            {
                taiKhoan.MaVaiTro = model.MaVaiTro;
            }
            taiKhoan.TrangThai = model.TrangThai;

            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                taiKhoan.MatKhau = PasswordHelper.Hash(model.MatKhauMoi.Trim());
            }

            if (taiKhoan.MaVaiTro == 1)
            {
                var quanLy = taiKhoan.QuanLy;
                if (quanLy == null)
                {
                    quanLy = new QuanLy { MaTK = taiKhoan.MaTK, HoTen = fullName, ChucVu = chucVuChuan ?? Permissions.StoreManager, TrangThai = taiKhoan.TrangThai };
                    _context.QuanLy.Add(quanLy);
                }
                quanLy.HoTen = fullName;
                quanLy.Email = email;
                quanLy.SoDienThoai = phone;
                quanLy.NgaySinh = model.NgaySinh;
                quanLy.GioiTinh = model.GioiTinh;
                quanLy.ChucVu = chucVuChuan ?? Permissions.StoreManager;
                quanLy.TrangThai = taiKhoan.TrangThai;
            }
            else
            {
                var khachHang = taiKhoan.KhachHang;
                if (khachHang == null)
                {
                    khachHang = new KhachHang
                    {
                        MaTK = taiKhoan.MaTK,
                        HoTen = fullName,
                        Email = email!,
                        NgaySinh = model.NgaySinh ?? taiKhoan.QuanLy?.NgaySinh,
                        GioiTinh = string.IsNullOrWhiteSpace(model.GioiTinh) ? taiKhoan.QuanLy?.GioiTinh : model.GioiTinh,
                        NgayDangKy = DateTime.Now
                    };
                    _context.KhachHang.Add(khachHang);
                }
                khachHang.HoTen = fullName;
                if (!string.IsNullOrWhiteSpace(email)) khachHang.Email = email;
                khachHang.SoDienThoai = phone;
                var isManagerToCustomer = previousRole == 1 && taiKhoan.MaVaiTro == 2;
                khachHang.NgaySinh = isManagerToCustomer
                    ? model.NgaySinh ?? taiKhoan.QuanLy?.NgaySinh ?? khachHang.NgaySinh
                    : model.NgaySinh;
                khachHang.GioiTinh = isManagerToCustomer
                    ? (string.IsNullOrWhiteSpace(model.GioiTinh) ? taiKhoan.QuanLy?.GioiTinh ?? khachHang.GioiTinh : model.GioiTinh)
                    : model.GioiTinh;
                khachHang.DiaChi = model.DiaChi;
                taiKhoan.KhachHang = khachHang;
            }

            if (previousRole == 2 && taiKhoan.MaVaiTro == 1 && taiKhoan.KhachHang != null && taiKhoan.QuanLy != null)
            {
                taiKhoan.QuanLy.NgaySinh = taiKhoan.KhachHang.NgaySinh;
                taiKhoan.QuanLy.GioiTinh = taiKhoan.KhachHang.GioiTinh;
            }

            await _context.SaveChangesAsync();

            if (taiKhoan.MaVaiTro == 2 && taiKhoan.KhachHang != null)
            {
                var selectedIds = (model.SoThichIds ?? new List<int>()).Distinct().ToList();
                var existingPreferences = await _context.KhachHangSoThich
                    .Where(preference => preference.MaKH == taiKhoan.KhachHang.MaKH)
                    .ToListAsync();
                var existingPreferenceIds = existingPreferences.Select(preference => preference.MaDM).ToList();
                var validCategories = selectedIds.Count == 0
                    ? new List<int>()
                    : await _context.DanhMuc
                        .Where(category => selectedIds.Contains(category.MaDM)
                            && (category.TrangThai || existingPreferenceIds.Contains(category.MaDM)))
                        .Select(category => category.MaDM)
                        .ToListAsync();
                if (validCategories.Count != selectedIds.Count)
                {
                    if (isAjax) return Json(new { success = false, message = "Danh sách sở thích có danh mục không hợp lệ. Hãy chọn danh mục đang hoạt động." });
                    TempData["ErrorMessage"] = "Danh sách sở thích có danh mục không hợp lệ. Hãy chọn danh mục đang hoạt động.";
                    return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
                }

                var toRemove = existingPreferences
                    .Where(preference => !selectedIds.Contains(preference.MaDM))
                    .ToList();
                var toAdd = selectedIds
                    .Where(categoryId => !existingPreferenceIds.Contains(categoryId))
                    .Select(categoryId => new KhachHangSoThich
                    {
                        MaKH = taiKhoan.KhachHang.MaKH,
                        MaDM = categoryId
                    })
                    .ToList();

                if (toRemove.Count > 0)
                {
                    _context.KhachHangSoThich.RemoveRange(toRemove);
                }
                if (toAdd.Count > 0)
                {
                    _context.KhachHangSoThich.AddRange(toAdd);
                }
                if (toRemove.Count > 0 || toAdd.Count > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }

            string currentTab = taiKhoan.MaVaiTro == 2 ? "KHACHHANG" : "QUANLY";
            TempData["SuccessMessage"] = $"Cập nhật hồ sơ tài khoản #{taiKhoan.TenDangNhap} thành công!";
            if (isAjax)
            {
                return Json(new { success = true, message = $"Cập nhật hồ sơ tài khoản #{taiKhoan.TenDangNhap} thành công!", redirectUrl = Url.Action(nameof(AccountManagement), new { accountType = currentTab }) });
            }
            return RedirectToAction(nameof(AccountManagement), new { accountType = currentTab });
        }

        // ==========================================
        // 8. KHÓA / MỞ KHÓA TÀI KHOẢN (TOGGLE STATUS)
        // ==========================================
        [Authorize(Policy = Permissions.PolicyCustomersDelete)]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            if (taiKhoan.MaVaiTro == 3 || (!Permissions.IsAdmin(User) && taiKhoan.MaVaiTro != 2))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Quản lý chỉ được khóa hoặc mở khóa tài khoản khách hàng." });
            }

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            if (taiKhoan.MaVaiTro == 1)
            {
                var manager = await _context.QuanLy.FirstOrDefaultAsync(item => item.MaTK == taiKhoan.MaTK);
                if (manager != null) manager.TrangThai = taiKhoan.TrangThai;
            }
            await _context.SaveChangesAsync();

            string stt = taiKhoan.TrangThai ? "Kích hoạt" : "Khóa";
            return Json(new { success = true, newStatus = taiKhoan.TrangThai, message = $"Đã {stt} tài khoản '{taiKhoan.TenDangNhap}'" });
        }

        // ==========================================
        // 9. XÓA TÀI KHOẢN / KHÁCH HÀNG (DELETE)
        // ==========================================
        [Authorize(Policy = Permissions.PolicyCustomersDelete)]
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
                return RedirectToAction(nameof(AccountManagement));
            }

            bool isAdmin = Permissions.IsAdmin(User);
            if (taiKhoan.MaTK == 1 || taiKhoan.TenDangNhap == "admin" || taiKhoan.MaVaiTro == 3)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Quản trị viên hệ thống (Admin).";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (!isAdmin && (taiKhoan.MaVaiTro != 2 || taiKhoan.KhachHang == null))
            {
                return Forbid();
            }

            string targetTab = taiKhoan.MaVaiTro == 1 ? "QUANLY" : "KHACHHANG";
            string originalUsername = taiKhoan.TenDangNhap;
            var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            bool isCustomer = taiKhoan.MaVaiTro == 2 && taiKhoan.KhachHang != null;
            if (isCustomer)
            {
                taiKhoan.KhachHang!.DaXoa = true;
                taiKhoan.TrangThai = false;
                string archivedUser = $"del_{taiKhoan.MaTK}_{stamp}";
                taiKhoan.TenDangNhap = archivedUser.Length > 50 ? archivedUser.Substring(0, 50) : archivedUser;
                string archivedEmail = $"del_{taiKhoan.KhachHang.MaKH}_{stamp}@deleted.local";
                taiKhoan.KhachHang.Email = archivedEmail.Length > 100 ? archivedEmail.Substring(0, 100) : archivedEmail;
            }

            if (isAdmin && taiKhoan.MaVaiTro == 1 && taiKhoan.QuanLy != null)
            {
                int maQL = taiKhoan.QuanLy.MaQL;
                var feedbacks = await _context.PhanHoi.Where(p => p.MaQL == maQL).ToListAsync();
                foreach (var fb in feedbacks)
                {
                    fb.MaQL = null;
                }

                bool hasSurveys = await _context.KhaoSat.AnyAsync(k => k.MaQL == maQL);
                if (!hasSurveys && taiKhoan.KhachHang == null)
                {
                    _context.QuanLy.Remove(taiKhoan.QuanLy);
                    _context.TaiKhoan.Remove(taiKhoan);
                }
                else
                {
                    taiKhoan.TrangThai = false;
                    taiKhoan.QuanLy.TrangThai = false;
                    string archivedUser = $"del_ql_{taiKhoan.MaTK}_{stamp}";
                    taiKhoan.TenDangNhap = archivedUser.Length > 50 ? archivedUser.Substring(0, 50) : archivedUser;
                    string archivedEmail = $"del_ql_{maQL}_{stamp}@deleted.local";
                    taiKhoan.QuanLy.Email = archivedEmail.Length > 100 ? archivedEmail.Substring(0, 100) : archivedEmail;
                    if (taiKhoan.KhachHang != null)
                    {
                        taiKhoan.KhachHang.DaXoa = true;
                        string archivedKhEmail = $"del_{taiKhoan.KhachHang.MaKH}_{stamp}@deleted.local";
                        taiKhoan.KhachHang.Email = archivedKhEmail.Length > 100 ? archivedKhEmail.Substring(0, 100) : archivedKhEmail;
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = isCustomer
                ? $"Đã xóa hồ sơ khách hàng '{originalUsername}' khỏi danh sách và khóa tài khoản."
                : $"Đã xóa tài khoản '{originalUsername}' thành công.";
            return RedirectToAction(nameof(AccountManagement), new { accountType = targetTab });
        }
    }
}

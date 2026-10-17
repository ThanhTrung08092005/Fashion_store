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
                    return RedirectToAction("Analytics", "Survey", new { area = "Admin" });

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
            HttpContext.Session.SetInt32("MaTK", taiKhoan.MaTK);
            HttpContext.Session.SetInt32("MaVaiTro", taiKhoan.MaVaiTro);
            if (taiKhoan.KhachHang != null)
            {
                HttpContext.Session.SetInt32("MaKH", taiKhoan.KhachHang.MaKH);
            }
            // =====================================================================

            return RedirectToAction("Analytics", "Survey", new { area = "Admin" });
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
        public async Task<IActionResult> AccountManagement(string? searchString, string? chucVu, int? roleId, bool? status, string accountType = "KHACHHANG")
        {
            if (string.IsNullOrEmpty(accountType) || (accountType != "QUANLY" && accountType != "KHACHHANG"))
            {
                accountType = "KHACHHANG";
            }

            ViewBag.IsAdmin = User.IsInRole("Admin") || User.HasClaim("MaVaiTro", "3");
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
                query = query.Where(t => t.MaVaiTro == 1 && t.QuanLy != null);
            }
            else
            {
                query = query.Where(t => t.MaVaiTro == 2 && t.KhachHang != null && !t.KhachHang.DaXoa);
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

            return View("~/Areas/Admin/Views/Account/AccountManagement.cshtml", result);
        }

        // Tương thích route Index cũ (/Admin/Account hoặc /Admin/Account/Index) -> chuyển hướng đến AccountManagement
        [HttpGet]
        public IActionResult Index(string? searchString, string? chucVu, int? roleId, bool? status, string accountType = "KHACHHANG")
        {
            return RedirectToAction(nameof(AccountManagement), new { searchString, chucVu, roleId, status, accountType });
        }

        // ==========================================
        // 5. THÊM TÀI KHOẢN KHÁCH HÀNG CRM MỚI
        // ==========================================
        [Authorize(Roles = "Quản lý, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CreateCustomerViewModel model)
        {
            if (!ModelState.IsValid || model.TenDangNhap.Any(char.IsWhiteSpace))
            {
                TempData["ErrorMessage"] = "Thông tin nhập vào chưa hợp lệ.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (exists)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã tồn tại.";
                return RedirectToAction(nameof(AccountManagement));
            }

            var vaiTroKH = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.Contains("Khách"))
                           ?? await _context.VaiTro.FirstOrDefaultAsync(v => v.MaVaiTro == 2);

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = PasswordHelper.Hash(model.MatKhau),
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
                NgayDangKy = DateTime.Now,
                DaXoa = false
            };

            _context.KhachHang.Add(khachHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm khách hàng '{model.HoTen}' thành công!";
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
            if (model.MaVaiTro != 1)
            {
                TempData["ErrorMessage"] = "Admin chỉ có thể tạo tài khoản nhân viên Quản lý tại màn hình này.";
                return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
            }

            if (!ModelState.IsValid || model.TenDangNhap.Any(char.IsWhiteSpace))
            {
                TempData["ErrorMessage"] = "Thông tin nhập vào chưa hợp lệ.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool exists = await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap);
            if (exists)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.TenDangNhap}' đã tồn tại.";
                return RedirectToAction(nameof(AccountManagement));
            }

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = PasswordHelper.Hash(model.MatKhau),
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
            return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
        }

        // ==========================================
        // 7. SỬA TÀI KHOẢN & PHÂN QUYỀN (EDIT)
        // ==========================================
        [Authorize(Roles = "Quản lý, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            var taiKhoan = await _context.TaiKhoan
                .Include(t => t.QuanLy)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.MaTK == model.MaTK);
            if (taiKhoan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa.";
                return RedirectToAction(nameof(AccountManagement));
            }

            bool isAdmin = User.IsInRole("Admin") || User.HasClaim("MaVaiTro", "3");
            int previousRole = taiKhoan.MaVaiTro;
            if (taiKhoan.MaVaiTro == 3)
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa tài khoản Quản trị viên hệ thống (Admin).";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (!isAdmin && (taiKhoan.MaVaiTro != 2 || model.MaVaiTro != 2 || !string.IsNullOrWhiteSpace(model.MatKhauMoi)))
            {
                return Forbid();
            }

            if (isAdmin && model.MaVaiTro is not (1 or 2))
            {
                TempData["ErrorMessage"] = "Vai trò có thể gán là Quản lý hoặc Khách hàng.";
                return RedirectToAction(nameof(AccountManagement));
            }

            if (isAdmin && previousRole == 1 && model.MaVaiTro == 2)
            {
                var birthDate = model.NgaySinh ?? taiKhoan.QuanLy?.NgaySinh ?? taiKhoan.KhachHang?.NgaySinh;
                var gender = string.IsNullOrWhiteSpace(model.GioiTinh)
                    ? taiKhoan.QuanLy?.GioiTinh ?? taiKhoan.KhachHang?.GioiTinh
                    : model.GioiTinh;
                var allowedGenders = new[] { "Nam", "Nữ", "Khác" };
                if (!birthDate.HasValue || birthDate.Value.Date > DateTime.Today.AddYears(-16).AddDays(-1)
                    || !allowedGenders.Contains(gender, StringComparer.Ordinal))
                {
                    TempData["ErrorMessage"] = "Cần nhập ngày sinh hợp lệ (trên 16 tuổi) và giới tính trước khi chuyển tài khoản quản lý thành khách hàng.";
                    return RedirectToAction(nameof(AccountManagement), new { accountType = "QUANLY" });
                }
            }

            if (isAdmin)
            {
                taiKhoan.MaVaiTro = model.MaVaiTro;
                taiKhoan.TrangThai = model.TrangThai;
                if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
                    taiKhoan.MatKhau = PasswordHelper.Hash(model.MatKhauMoi);
            }

            if (taiKhoan.MaVaiTro == 1)
            {
                var quanLy = taiKhoan.QuanLy;
                if (quanLy == null)
                {
                    quanLy = new QuanLy { MaTK = taiKhoan.MaTK, HoTen = model.HoTen, ChucVu = model.ChucVu ?? "Quản lý CRM", TrangThai = taiKhoan.TrangThai };
                    _context.QuanLy.Add(quanLy);
                }
                quanLy.HoTen = model.HoTen;
                quanLy.Email = model.Email;
                quanLy.SoDienThoai = model.SoDienThoai;
                quanLy.NgaySinh = model.NgaySinh;
                quanLy.GioiTinh = model.GioiTinh;
                quanLy.ChucVu = model.ChucVu ?? "Quản lý CRM";
                quanLy.TrangThai = taiKhoan.TrangThai;
            }
            else
            {
                var khachHang = taiKhoan.KhachHang;
                if (khachHang == null)
                {
                    if (string.IsNullOrWhiteSpace(model.Email))
                    {
                        TempData["ErrorMessage"] = "Khách hàng cần có email hợp lệ.";
                        return RedirectToAction(nameof(AccountManagement));
                    }
                    khachHang = new KhachHang
                    {
                        MaTK = taiKhoan.MaTK,
                        HoTen = model.HoTen,
                        Email = model.Email,
                        NgaySinh = model.NgaySinh ?? taiKhoan.QuanLy?.NgaySinh,
                        GioiTinh = string.IsNullOrWhiteSpace(model.GioiTinh) ? taiKhoan.QuanLy?.GioiTinh : model.GioiTinh,
                        NgayDangKy = DateTime.Now
                    };
                    _context.KhachHang.Add(khachHang);
                }
                khachHang.HoTen = model.HoTen;
                if (!string.IsNullOrWhiteSpace(model.Email)) khachHang.Email = model.Email;
                khachHang.SoDienThoai = model.SoDienThoai;
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
            if (previousRole == 2 && taiKhoan.KhachHang != null)
            {
                var selectedIds = (model.SoThichIds ?? new List<int>()).Distinct().ToList();
                var existingPreferences = await _context.KhachHangSoThich
                    .Where(preference => preference.MaKH == taiKhoan.KhachHang.MaKH)
                    .ToListAsync();
                var existingPreferenceIds = existingPreferences.Select(preference => preference.MaDM).ToList();
                var validCategories = await _context.DanhMuc
                    .Where(category => selectedIds.Contains(category.MaDM)
                        && (category.TrangThai || existingPreferenceIds.Contains(category.MaDM)))
                    .Select(category => category.MaDM)
                    .ToListAsync();
                if (validCategories.Count != selectedIds.Count)
                {
                    TempData["ErrorMessage"] = "Danh sách sở thích có danh mục không hợp lệ. Hãy chọn danh mục đang hoạt động.";
                    return RedirectToAction(nameof(AccountManagement), new { accountType = "KHACHHANG" });
                }

                _context.KhachHangSoThich.RemoveRange(existingPreferences);
                _context.KhachHangSoThich.AddRange(selectedIds.Select(categoryId => new KhachHangSoThich
                {
                    MaKH = taiKhoan.KhachHang.MaKH,
                    MaDM = categoryId
                }));
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật phân quyền & thông tin thành công!";
            string targetTab = taiKhoan.MaVaiTro == 1 ? "QUANLY" : "KHACHHANG";
            return RedirectToAction(nameof(AccountManagement), new { accountType = targetTab });
        }

        // ==========================================
        // 8. KHÓA / MỞ KHÓA TÀI KHOẢN (TOGGLE STATUS)
        // ==========================================
        [Authorize(Roles = "Quản lý, Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            if (taiKhoan.MaVaiTro == 3 || (!User.IsInRole("Admin") && taiKhoan.MaVaiTro != 2))
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
        [Authorize(Roles = "Quản lý, Admin")]
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

            bool isAdmin = User.IsInRole("Admin") || User.HasClaim("MaVaiTro", "3");
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

            bool isCustomer = taiKhoan.MaVaiTro == 2 && taiKhoan.KhachHang != null;
            if (isCustomer)
            {
                taiKhoan.KhachHang!.DaXoa = true;
                taiKhoan.TrangThai = false;
            }

            if (isAdmin && taiKhoan.MaVaiTro == 1 && taiKhoan.QuanLy != null)
            {
                _context.QuanLy.Remove(taiKhoan.QuanLy);
                _context.TaiKhoan.Remove(taiKhoan);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = isCustomer
                ? $"Đã xóa hồ sơ khách hàng '{taiKhoan.TenDangNhap}' khỏi danh sách và khóa tài khoản."
                : $"Đã xóa tài khoản '{taiKhoan.TenDangNhap}' thành công.";
            return RedirectToAction(nameof(AccountManagement), new { accountType = targetTab });
        }
    }
}

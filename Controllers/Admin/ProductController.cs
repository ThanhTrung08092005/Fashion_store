using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using Fashion_store.ViewModels;

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Quản lý, Admin")]
    public class ProductController : Controller
    {
        private readonly FashionStoreDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductController(FashionStoreDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==========================================
        // 1. TRANG QUáº¢N LÃ Sáº¢N PHáº¨M & DANH Má»¤C (PRODUCT MANAGEMENT)
        // ==========================================
        [HttpGet]
        [ActionName("ProductManagement")]
        public async Task<IActionResult> ProductManagement(
            string tab = "SANPHAM",
            string? searchString = null,
            int? maDM = null,
            string? size = null,
            string? color = null,
            string? priceRange = null,
            string? stockStatus = null,
            bool? status = null,
            string sortBy = "date_desc")
        {
            // Danh sÃ¡ch dá»¯ liá»‡u há»— trá»£ bá»™ lá»c & modal
            var danhMucs = await _context.DanhMuc.Where(d => d.TrangThai).OrderBy(d => d.TenDM).ToListAsync();

            ViewBag.CurrentTab = tab;
            ViewBag.DanhMucList = danhMucs;

            // Thá»‘ng kÃª KPI Ä‘áº§u trang
            ViewBag.TotalProducts = await _context.SanPham.CountAsync();
            ViewBag.TotalStock = await _context.SoLuongSp.SumAsync(s => (int?)s.SoLuongTon) ?? 0;
            ViewBag.LowStockCount = await _context.SoLuongSp.CountAsync(s => s.SoLuongTon <= 10);
            ViewBag.TotalCategories = danhMucs.Count;

            // Danh sÃ¡ch táº¥t cáº£ Size vÃ  MÃ u thá»±c táº¿ trong kho
            ViewBag.AllSizes = await _context.SoLuongSp.Select(s => s.KichThuoc).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.AllColors = await _context.SoLuongSp.Select(s => s.MauSac).Distinct().OrderBy(s => s).ToListAsync();

            // LÆ°u tráº¡ng thÃ¡i lá»c Ä‘á»ƒ fill láº¡i form
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentMaDM = maDM;
            ViewBag.CurrentSize = size;
            ViewBag.CurrentColor = color;
            ViewBag.CurrentPriceRange = priceRange;
            ViewBag.CurrentStockStatus = stockStatus;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSortBy = sortBy;

            // Dá»¯ liá»‡u cho Tab Danh Má»¥c
            ViewBag.Categories = await _context.DanhMuc
                .Include(d => d.SanPhams)
                .OrderBy(d => d.TenDM)
                .ToListAsync();

            // Truy váº¥n danh sÃ¡ch Sáº£n pháº©m Ão kÃ¨m biáº¿n thá»ƒ, danh má»¥c
            var query = _context.SanPham
                .Where(s => !s.DaXoa)
                .Include(s => s.DanhMuc)
                .Include(s => s.SoLuongSps)
                .AsQueryable();

            // 1. TÃ¬m kiáº¿m nÃ¢ng cao (TÃªn Ã¡o, SKU, Cháº¥t liá»‡u)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string kw = searchString.Trim().ToLower();
                query = query.Where(s => s.TenSP.ToLower().Contains(kw)
                    || (s.ChatLieu != null && s.ChatLieu.ToLower().Contains(kw))
                    || s.SoLuongSps.Any(b => b.SKU.ToLower().Contains(kw)));
            }

            // 2. Lá»c theo Danh má»¥c
            if (maDM.HasValue)
            {
                query = query.Where(s => s.MaDM == maDM.Value);
            }

            // 3. Lá»c theo KÃ­ch thÆ°á»›c
            if (!string.IsNullOrWhiteSpace(size))
            {
                query = query.Where(s => s.SoLuongSps.Any(b => b.KichThuoc == size));
            }

            // 5. Lá»c theo MÃ u sáº¯c
            if (!string.IsNullOrWhiteSpace(color))
            {
                query = query.Where(s => s.SoLuongSps.Any(b => b.MauSac == color));
            }

            // 6. Lá»c theo Khoáº£ng giÃ¡
            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "under200":
                        query = query.Where(s => s.SoLuongSps.Any(b => b.GiaBan < 200000));
                        break;
                    case "200to400":
                        query = query.Where(s => s.SoLuongSps.Any(b => b.GiaBan >= 200000 && b.GiaBan <= 400000));
                        break;
                    case "above400":
                        query = query.Where(s => s.SoLuongSps.Any(b => b.GiaBan > 400000));
                        break;
                }
            }

            // 7. Lá»c theo Tá»“n kho (CÃ²n hÃ ng / Háº¿t hÃ ng)
            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                switch (stockStatus)
                {
                    case "in_stock":
                        query = query.Where(s => s.SoLuongSps.Sum(b => b.SoLuongTon) > 0);
                        break;
                    case "out_stock":
                        query = query.Where(s => s.SoLuongSps.Sum(b => b.SoLuongTon) == 0);
                        break;
                }
            }

            // 8. Lá»c theo Tráº¡ng thÃ¡i kinh doanh
            if (status.HasValue)
            {
                query = query.Where(s => s.TrangThai == status.Value);
            }

            // 9. Sáº¯p xáº¿p (Sorting)
            query = sortBy switch
            {
                "date_asc" => query.OrderBy(s => s.NgayTao),
                "price_asc" => query.OrderBy(s => s.SoLuongSps.Min(b => b.GiaBan)),
                "price_desc" => query.OrderByDescending(s => s.SoLuongSps.Max(b => b.GiaBan)),
                "stock_asc" => query.OrderBy(s => s.SoLuongSps.Sum(b => b.SoLuongTon)),
                "stock_desc" => query.OrderByDescending(s => s.SoLuongSps.Sum(b => b.SoLuongTon)),
                "name_asc" => query.OrderBy(s => s.TenSP),
                "name_desc" => query.OrderByDescending(s => s.TenSP),
                _ => query.OrderByDescending(s => s.NgayTao)
            };

            var productList = await query.Select(s => new ProductItemViewModel
            {
                MaSP = s.MaSP,
                TenSP = s.TenSP,
                MaDM = s.MaDM,
                TenDanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDM : "ChÆ°a phÃ¢n loáº¡i",
                ChatLieu = s.ChatLieu,
                MoTa = s.MoTa,
                HinhAnh = s.HinhAnh,
                HinhAnhMoTa = s.HinhAnhMoTa,
                TrangThai = s.TrangThai,
                NgayTao = s.NgayTao,
                BienThes = s.SoLuongSps.ToList()
            }).ToListAsync();

            return View("~/Areas/Admin/Views/Product/ProductManagement.cshtml", productList);
        }

        // TÆ°Æ¡ng thÃ­ch route Index cÅ© (/Admin/Product hoáº·c /Admin/Product/Index) -> chuyá»ƒn hÆ°á»›ng Ä‘áº¿n ProductManagement
        [HttpGet]
        public IActionResult Index(
            string tab = "SANPHAM",
            string? searchString = null,
            int? maDM = null,
            string? size = null,
            string? color = null,
            string? priceRange = null,
            string? stockStatus = null,
            bool? status = null,
            string sortBy = "date_desc")
        {
            return RedirectToAction(nameof(ProductManagement), new { tab, searchString, maDM, size, color, priceRange, stockStatus, status, sortBy });
        }

        // ==========================================
        // 2. THÃŠM / Cáº¬P NHáº¬T Sáº¢N PHáº¨M ÃO
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(12 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
        public async Task<IActionResult> SaveProduct(ProductManageViewModel model)
        {
            var thumbnailFile = model.ThumbnailImageFile ?? model.InitialImageFile;
            var thumbnailPath = string.IsNullOrWhiteSpace(model.HinhAnh) ? null : model.HinhAnh.Trim();
            var detailPath = string.IsNullOrWhiteSpace(model.HinhAnhMoTa) ? null : model.HinhAnhMoTa.Trim();
            if (HasInvalidProductImage(thumbnailFile) || HasInvalidProductImage(model.DetailImageFile)
                || !IsValidProductImagePath(thumbnailPath) || !IsValidProductImagePath(detailPath))
            {
                TempData["ErrorMessage"] = "Ảnh phải là JPG, PNG hoặc WEBP không quá 5 MB; đường dẫn ảnh phải bắt đầu bằng / hoặc http(s)://.";
                return RedirectToAction(nameof(ProductManagement));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin sản phẩm chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(ProductManagement));
            }

            try
            {
                if (!await _context.DanhMuc.AnyAsync(category => category.MaDM == model.MaDM && category.TrangThai))
                {
                    TempData["ErrorMessage"] = "Danh mục không tồn tại hoặc đang bị khóa.";
                    return RedirectToAction(nameof(ProductManagement));
                }

                var uploadedThumbnailPath = await SaveProductImageAsync(thumbnailFile);
                var uploadedDetailPath = await SaveProductImageAsync(model.DetailImageFile);
                var savedThumbnailPath = uploadedThumbnailPath ?? thumbnailPath;
                var savedDetailPath = uploadedDetailPath ?? detailPath;
                if (model.MaSP.HasValue && model.MaSP > 0)
                {
                    // Cáº­p nháº­t sáº£n pháº©m cÅ©
                    var sp = await _context.SanPham.Include(s => s.SoLuongSps).FirstOrDefaultAsync(s => s.MaSP == model.MaSP.Value);
                    if (sp == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần cập nhật.";
                        return RedirectToAction(nameof(ProductManagement));
                    }

                    sp.TenSP = model.TenSP.Trim();
                    sp.MaDM = model.MaDM;
                    sp.ChatLieu = model.ChatLieu?.Trim();
                    sp.MoTa = model.MoTa?.Trim();
                    sp.HinhAnh = savedThumbnailPath ?? sp.HinhAnh;
                    sp.HinhAnhMoTa = savedDetailPath ?? sp.HinhAnhMoTa;
                    sp.TrangThai = model.TrangThai;

                    // Cáº­p nháº­t thÃ´ng sá»‘ biáº¿n thá»ƒ náº¿u sáº£n pháº©m chá»‰ cÃ³ 1 biáº¿n thá»ƒ duy nháº¥t
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật mẫu áo '{sp.TenSP}' thành công.";
                }
                else
                {
                    // ThÃªm má»›i sáº£n pháº©m
                    var sp = new SanPham
                    {
                        TenSP = model.TenSP.Trim(),
                        MaDM = model.MaDM,
                        ChatLieu = model.ChatLieu?.Trim(),
                        MoTa = model.MoTa?.Trim(),
                        HinhAnh = savedThumbnailPath,
                        HinhAnhMoTa = savedDetailPath,
                        TrangThai = model.TrangThai,
                        NgayTao = DateTime.Now
                    };

                    _context.SanPham.Add(sp);
                    await _context.SaveChangesAsync();

                    // Táº¡o biáº¿n thá»ƒ ban Ä‘áº§u náº¿u ngÆ°á»i dÃ¹ng cÃ³ nháº­p
                    string sku = !string.IsNullOrWhiteSpace(model.InitialSKU) 
                        ? model.InitialSKU.Trim().ToUpper() 
                        : $"SP{sp.MaSP}-{model.InitialSize?.ToUpper()}-{DateTime.Now:fff}";

                    var variant = new SoLuongSp
                    {
                        MaSP = sp.MaSP,
                        SKU = sku,
                        KichThuoc = !string.IsNullOrWhiteSpace(model.InitialSize) ? model.InitialSize.Trim().ToUpper() : "M",
                        MauSac = !string.IsNullOrWhiteSpace(model.InitialColor) ? model.InitialColor.Trim() : "Đen",
                        GiaBan = model.InitialPrice ?? 250000,
                        SoLuongTon = model.InitialStock ?? 50,
                        HinhAnh = savedThumbnailPath,
                        TrangThai = true
                    };

                    _context.SoLuongSp.Add(variant);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã thêm mẫu áo '{sp.TenSP}' thành công, kèm biến thể size {variant.KichThuoc}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Không thể lưu sản phẩm: {ex.Message}";
            }

            return RedirectToAction(nameof(ProductManagement));
        }

        private async Task<string?> SaveProductImageAsync(Microsoft.AspNetCore.Http.IFormFile? image)
        {
            if (image == null || image.Length == 0) return null;
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "products");
            Directory.CreateDirectory(uploadDirectory);
            var filePath = Path.Combine(uploadDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await image.CopyToAsync(stream);
            return $"/uploads/products/{fileName}";
        }

        private static bool HasInvalidProductImage(Microsoft.AspNetCore.Http.IFormFile? image)
        {
            if (image is not { Length: > 0 }) return false;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            return image.Length > 5L * 1024 * 1024
                || !allowedExtensions.Contains(Path.GetExtension(image.FileName).ToLowerInvariant());
        }

        private static bool IsValidProductImagePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            if (path.Length > 500) return false;

            if (path.StartsWith("/", StringComparison.Ordinal))
            {
                return !path.StartsWith("//", StringComparison.Ordinal)
                    && !path.Contains('\\')
                    && !path.Split('/', StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal);
            }

            return Uri.TryCreate(path, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        // ==========================================
        // 3. Äá»”I TRáº NG THÃI KINH DOANH (Báº¬T / Táº®T)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var sp = await _context.SanPham.FindAsync(id);
            if (sp == null)
            {
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m.";
                return RedirectToAction(nameof(ProductManagement));
            }

            sp.TrangThai = !sp.TrangThai;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ÄÃ£ {(sp.TrangThai ? "báº­t kinh doanh" : "táº¡m ngá»«ng bÃ¡n")} máº«u Ã¡o '{sp.TenSP}'.";
            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 4. XÃ“A Sáº¢N PHáº¨M ÃO (Xá»¬ LÃ RÃ€NG BUá»˜C TOÃ€N Váº¸N Dá»® LIá»†U)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var product = await _context.SanPham.FirstOrDefaultAsync(item => item.MaSP == id && !item.DaXoa);
            if (product == null)
            {
                if (isAjax) return NotFound(new { success = false, message = "Không tìm thấy sản phẩm." });
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần ẩn.";
                return RedirectToAction(nameof(ProductManagement));
            }

            product.DaXoa = true;
            product.TrangThai = false;
            await _context.SaveChangesAsync();
            string message = $"Đã ẩn sản phẩm '{product.TenSP}'. Lịch sử đơn hàng và phản hồi được giữ nguyên.";
            if (isAjax) return Json(new { success = true, message });
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 5. API Láº¤Y CHI TIáº¾T BIáº¾N THá»‚ Cá»¦A ÃO (JSON)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var sp = await _context.SanPham
                .Include(s => s.DanhMuc)
                .Include(s => s.SoLuongSps)
                .FirstOrDefaultAsync(s => s.MaSP == productId);

            if (sp == null)
            {
                return NotFound(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m" });
            }

            var result = new
            {
                success = true,
                maSP = sp.MaSP,
                tenSP = sp.TenSP,
                tenDM = sp.DanhMuc?.TenDM ?? "ChÆ°a phÃ¢n loáº¡i",
                chatLieu = sp.ChatLieu ?? "",
                moTa = sp.MoTa ?? "",
                hinhAnh = sp.HinhAnh,
                hinhAnhMoTa = sp.HinhAnhMoTa,
                trangThai = sp.TrangThai,
                maDM = sp.MaDM,
                variants = sp.SoLuongSps.Select(v => new
                {
                    maSL = v.MaSL,
                    sku = v.SKU,
                    kichThuoc = v.KichThuoc,
                    mauSac = v.MauSac,
                    giaBan = v.GiaBan,
                    soLuongTon = v.SoLuongTon,
                    hinhAnh = v.HinhAnh,
                    trangThai = v.TrangThai
                }).ToList()
            };

            return Json(result);
        }

        // ==========================================
        // 6. THÃŠM / Cáº¬P NHáº¬T BIáº¾N THá»‚ (SIZE / MÃ€U)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveVariant(VariantManageViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                if (isAjax) return Json(new { success = false, message = errors });
                TempData["ErrorMessage"] = errors;
                return RedirectToAction(nameof(ProductManagement));
            }

            try
            {
                if (model.MaSL.HasValue && model.MaSL.Value > 0)
                {
                    var v = await _context.SoLuongSp.FindAsync(model.MaSL.Value);
                    if (v == null)
                    {
                        if (isAjax) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ." });
                        TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ.";
                        return RedirectToAction(nameof(ProductManagement));
                    }

                    v.SKU = model.SKU.Trim().ToUpper();
                    v.KichThuoc = model.KichThuoc.Trim().ToUpper();
                    v.MauSac = model.MauSac.Trim();
                    v.GiaBan = model.GiaBan;
                    v.SoLuongTon = model.SoLuongTon;
                    v.HinhAnh = model.HinhAnh?.Trim();
                    v.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    if (isAjax) return Json(new { success = true, message = $"ÄÃ£ cáº­p nháº­t biáº¿n thá»ƒ SKU '{v.SKU}' thÃ nh cÃ´ng!" });
                    TempData["SuccessMessage"] = $"ÄÃ£ cáº­p nháº­t biáº¿n thá»ƒ SKU '{v.SKU}' thÃ nh cÃ´ng!";
                }
                else
                {
                    bool existsSku = await _context.SoLuongSp.AnyAsync(s => s.SKU == model.SKU.Trim().ToUpper());
                    if (existsSku)
                    {
                        if (isAjax) return Json(new { success = false, message = $"MÃ£ SKU '{model.SKU}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng." });
                        TempData["ErrorMessage"] = $"MÃ£ SKU '{model.SKU}' Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.";
                        return RedirectToAction(nameof(ProductManagement));
                    }

                    var v = new SoLuongSp
                    {
                        MaSP = model.MaSP,
                        SKU = model.SKU.Trim().ToUpper(),
                        KichThuoc = model.KichThuoc.Trim().ToUpper(),
                        MauSac = model.MauSac.Trim(),
                        GiaBan = model.GiaBan,
                        SoLuongTon = model.SoLuongTon,
                        HinhAnh = model.HinhAnh?.Trim(),
                        TrangThai = true
                    };

                    _context.SoLuongSp.Add(v);
                    await _context.SaveChangesAsync();
                    if (isAjax) return Json(new { success = true, message = $"ÄÃ£ thÃªm má»›i biáº¿n thá»ƒ Size {v.KichThuoc} - MÃ u {v.MauSac} thÃ nh cÃ´ng!" });
                    TempData["SuccessMessage"] = $"ÄÃ£ thÃªm má»›i biáº¿n thá»ƒ Size {v.KichThuoc} - MÃ u {v.MauSac} thÃ nh cÃ´ng!";
                }
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = $"Lá»—i: {ex.Message}";
            }

            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 7. XÃ“A BIáº¾N THá»‚ SIZE/MÃ€U
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            var v = await _context.SoLuongSp.FindAsync(id);
            if (v == null)
            {
                if (isAjax) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ." });
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ.";
                return RedirectToAction(nameof(ProductManagement));
            }

            string sku = v.SKU;
            _context.SoLuongSp.Remove(v);
            await _context.SaveChangesAsync();

            if (isAjax) return Json(new { success = true, message = $"ÄÃ£ xÃ³a biáº¿n thá»ƒ SKU '{sku}' thÃ nh cÃ´ng." });
            TempData["SuccessMessage"] = $"ÄÃ£ xÃ³a biáº¿n thá»ƒ SKU '{sku}' thÃ nh cÃ´ng.";
            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 8. Cáº¬P NHáº¬T NHANH Tá»’N KHO BIáº¾N THá»‚
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int maSL, int soLuongTon)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            if (soLuongTon < 0)
            {
                if (isAjax) return Json(new { success = false, message = "Sá»‘ lÆ°á»£ng tá»“n khÃ´ng thá»ƒ lÃ  sá»‘ Ã¢m." });
                TempData["ErrorMessage"] = "Sá»‘ lÆ°á»£ng tá»“n khÃ´ng thá»ƒ lÃ  sá»‘ Ã¢m.";
                return RedirectToAction(nameof(ProductManagement));
            }

            var v = await _context.SoLuongSp.FindAsync(maSL);
            if (v == null)
            {
                if (isAjax) return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ." });
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y biáº¿n thá»ƒ.";
                return RedirectToAction(nameof(ProductManagement));
            }

            v.SoLuongTon = soLuongTon;
            await _context.SaveChangesAsync();

            if (isAjax)
            {
                var totalStock = await _context.SoLuongSp.Where(s => s.MaSP == v.MaSP).SumAsync(s => s.SoLuongTon);
                return Json(new { success = true, message = $"ÄÃ£ cáº­p nháº­t tá»“n kho biáº¿n thá»ƒ SKU '{v.SKU}' thÃ nh {soLuongTon} cÃ¡i.", totalStock = totalStock });
            }

            TempData["SuccessMessage"] = $"ÄÃ£ cáº­p nháº­t tá»“n kho biáº¿n thá»ƒ SKU '{v.SKU}' thÃ nh {soLuongTon} cÃ¡i.";
            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 9. QUáº¢N LÃ DANH Má»¤C ÃO (CRUD)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(CategoryManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống.";
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            try
            {
                string categoryName = model.TenDM.Trim();
                bool duplicateName = await _context.DanhMuc.AnyAsync(category =>
                    category.MaDM != (model.MaDM ?? 0) && category.TenDM.ToLower() == categoryName.ToLower());
                if (duplicateName)
                {
                    TempData["ErrorMessage"] = $"Danh mục '{categoryName}' đã tồn tại.";
                    return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
                }

                if (model.MaDM.HasValue && model.MaDM.Value > 0)
                {
                    var dm = await _context.DanhMuc.FindAsync(model.MaDM.Value);
                    if (dm == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                        return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
                    }

                    dm.TenDM = categoryName;
                    dm.MoTa = model.MoTa?.Trim();
                    dm.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{dm.TenDM}' thành công.";
                }
                else
                {
                    var dm = new DanhMuc
                    {
                        TenDM = categoryName,
                        MoTa = model.MoTa?.Trim(),
                        TrangThai = model.TrangThai
                    };

                    _context.DanhMuc.Add(dm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã thêm mới danh mục '{dm.TenDM}' thành công.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Không thể lưu danh mục: {ex.Message}";
            }

            return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.DanhMuc.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            category.TrangThai = !category.TrangThai;
            await _context.SaveChangesAsync();
            TempData[category.TrangThai ? "SuccessMessage" : "ErrorMessage"] = category.TrangThai
                ? $"Đã mở danh mục '{category.TenDM}' cho khách hàng."
                : $"Đã khóa danh mục '{category.TenDM}'. Danh mục này sẽ không hiển thị cho khách hàng.";
            return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var dm = await _context.DanhMuc.Include(d => d.SanPhams).FirstOrDefaultAsync(d => d.MaDM == id);
            if (dm == null)
            {
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y danh má»¥c.";
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            if (dm.SanPhams.Any())
            {
                TempData["ErrorMessage"] = $"KhÃ´ng thá»ƒ xÃ³a danh má»¥c '{dm.TenDM}' vÃ¬ Ä‘ang cÃ³ {dm.SanPhams.Count} sáº£n pháº©m liÃªn káº¿t.";
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            string tenDM = dm.TenDM;
            _context.DanhMuc.Remove(dm);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ÄÃ£ xÃ³a danh má»¥c '{tenDM}' thÃ nh cÃ´ng.";
            return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
        }
    }
}


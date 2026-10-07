using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;
using Fashion_store.Models;
using Fashion_store.ViewModels;

namespace Fashion_store.Controllers.Admin
{
    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly FashionStoreDbContext _context;

        public ProductController(FashionStoreDbContext context)
        {
            _context = context;
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
            var danhMucs = await _context.DanhMuc.OrderBy(d => d.TenDM).ToListAsync();

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
        public async Task<IActionResult> SaveProduct(ProductManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "ThÃ´ng tin sáº£n pháº©m chÆ°a há»£p lá»‡. Vui lÃ²ng kiá»ƒm tra láº¡i.";
                return RedirectToAction(nameof(ProductManagement));
            }

            try
            {
                if (model.MaSP.HasValue && model.MaSP > 0)
                {
                    // Cáº­p nháº­t sáº£n pháº©m cÅ©
                    var sp = await _context.SanPham.Include(s => s.SoLuongSps).FirstOrDefaultAsync(s => s.MaSP == model.MaSP.Value);
                    if (sp == null)
                    {
                        TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m cáº§n cáº­p nháº­t.";
                        return RedirectToAction(nameof(ProductManagement));
                    }

                    sp.TenSP = model.TenSP.Trim();
                    sp.MaDM = model.MaDM;
                    sp.ChatLieu = model.ChatLieu?.Trim();
                    sp.MoTa = model.MoTa?.Trim();
                    sp.TrangThai = model.TrangThai;

                    // Cáº­p nháº­t thÃ´ng sá»‘ biáº¿n thá»ƒ náº¿u sáº£n pháº©m chá»‰ cÃ³ 1 biáº¿n thá»ƒ duy nháº¥t
                    if (sp.SoLuongSps.Count == 1 && !string.IsNullOrWhiteSpace(model.InitialSize))
                    {
                        var primaryVar = sp.SoLuongSps.First();
                        primaryVar.KichThuoc = model.InitialSize.Trim().ToUpper();
                        if (!string.IsNullOrWhiteSpace(model.InitialColor)) primaryVar.MauSac = model.InitialColor.Trim();
                        if (!string.IsNullOrWhiteSpace(model.InitialSKU)) primaryVar.SKU = model.InitialSKU.Trim().ToUpper();
                        if (model.InitialPrice.HasValue) primaryVar.GiaBan = model.InitialPrice.Value;
                        if (model.InitialStock.HasValue) primaryVar.SoLuongTon = model.InitialStock.Value;
                        if (!string.IsNullOrWhiteSpace(model.InitialImage)) primaryVar.HinhAnh = model.InitialImage.Trim();
                    }
                    else if (sp.SoLuongSps.Count == 0 && !string.IsNullOrWhiteSpace(model.InitialSize))
                    {
                        var newVar = new SoLuongSp
                        {
                            MaSP = sp.MaSP,
                            SKU = !string.IsNullOrWhiteSpace(model.InitialSKU) ? model.InitialSKU.Trim().ToUpper() : $"SP{sp.MaSP}-{model.InitialSize?.ToUpper() ?? "M"}",
                            KichThuoc = model.InitialSize.Trim().ToUpper(),
                            MauSac = !string.IsNullOrWhiteSpace(model.InitialColor) ? model.InitialColor.Trim() : "Äen",
                            GiaBan = model.InitialPrice ?? 250000,
                            SoLuongTon = model.InitialStock ?? 50,
                            HinhAnh = model.InitialImage?.Trim(),
                            TrangThai = true
                        };
                        _context.SoLuongSp.Add(newVar);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"ÄÃ£ cáº­p nháº­t thÃ´ng tin Ã¡o '{sp.TenSP}' thÃ nh cÃ´ng!";
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
                        MauSac = !string.IsNullOrWhiteSpace(model.InitialColor) ? model.InitialColor.Trim() : "Äen",
                        GiaBan = model.InitialPrice ?? 250000,
                        SoLuongTon = model.InitialStock ?? 50,
                        HinhAnh = model.InitialImage?.Trim(),
                        TrangThai = true
                    };

                    _context.SoLuongSp.Add(variant);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"ThÃªm má»›i máº«u Ã¡o '{sp.TenSP}' thÃ nh cÃ´ng kÃ¨m biáº¿n thá»ƒ size {variant.KichThuoc}!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lá»—i khi lÆ°u sáº£n pháº©m: {ex.Message}";
            }

            return RedirectToAction(nameof(ProductManagement));
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
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return Forbid();

            var sp = await _context.SanPham
                .Include(s => s.SoLuongSps)
                .FirstOrDefaultAsync(s => s.MaSP == id);

            if (sp == null)
            {
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m Ä‘á»ƒ xÃ³a.";
                return RedirectToAction(nameof(ProductManagement));
            }

            string tenSP = sp.TenSP;

            try
            {
                // RÃ€NG BUá»˜C 1: KIá»‚M TRA ÄÆ N HÃ€NG (CHITIETDONHANG)
                // Tuyá»‡t Ä‘á»‘i khÃ´ng xÃ³a cá»©ng sáº£n pháº©m Ä‘Ã£ bÃ¡n Ä‘á»ƒ báº£o vá»‡ lá»‹ch sá»­ hÃ³a Ä‘Æ¡n & doanh thu
                try
                {
                    var sqlCheckOrder = @"
                        SELECT COUNT(*) 
                        FROM CHITIETDONHANG c 
                        INNER JOIN SOLUONGSP s ON c.MaSL = s.MaSL 
                        WHERE s.MaSP = {0}";
                    var orderCount = await _context.Database.SqlQueryRaw<int>(sqlCheckOrder, id).FirstOrDefaultAsync();
                    if (orderCount > 0)
                    {
                        TempData["ErrorMessage"] = $"KhÃ´ng thá»ƒ xÃ³a máº«u Ã¡o '{tenSP}' vÃ¬ Ä‘Ã£ cÃ³ {orderCount} lÆ°á»£t Ä‘áº·t hÃ ng trong lá»‹ch sá»­! Äá»ƒ báº£o toÃ n hÃ³a Ä‘Æ¡n vÃ  doanh thu, vui lÃ²ng sá»­ dá»¥ng nÃºt [Táº¡m áº©n] thay vÃ¬ xÃ³a cá»©ng.";
                        return RedirectToAction(nameof(ProductManagement));
                    }
                }
                catch
                {
                    // Tiáº¿p tá»¥c náº¿u báº£ng Ä‘Æ¡n hÃ ng chÆ°a sáºµn sÃ ng
                }

                // RÃ€NG BUá»˜C 2: PHáº¢N Há»’I Ã KIáº¾N KHÃCH HÃ€NG (PHANHOI)
                // Cá»™t MaSP cho phÃ©p NULL, gá»¡ liÃªn káº¿t mÃ£ Ã¡o trÆ°á»›c Ä‘á»ƒ lÆ°u giá»¯ gÃ³p Ã½ cá»§a khÃ¡ch hÃ ng
                var relatedFeedbacks = await _context.PhanHoi.Where(p => p.MaSP == id).ToListAsync();
                if (relatedFeedbacks.Any())
                {
                    foreach (var fb in relatedFeedbacks)
                    {
                        fb.MaSP = null;
                    }
                }

                // RÃ€NG BUá»˜C 3: ÄÃNH GIÃ Sáº¢N PHáº¨M (DANHGIA)
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM DANHGIA WHERE MaSP = {0}", id);
                }
                catch
                {
                    // Tiáº¿p tá»¥c náº¿u báº£ng Ä‘Ã¡nh giÃ¡ chÆ°a cÃ³ dá»¯ liá»‡u
                }

                // XÃ³a sáº£n pháº©m Ã¡o (CÃ¡c biáº¿n thá»ƒ trong SOLUONGSP sáº½ Ä‘Æ°á»£c tá»± Ä‘á»™ng xÃ³a cascade)
                _context.SanPham.Remove(sp);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"ÄÃ£ xÃ³a thÃ nh cÃ´ng máº«u Ã¡o '{tenSP}' vÃ  toÃ n bá»™ biáº¿n thá»ƒ tá»“n kho liÃªn quan.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"KhÃ´ng thá»ƒ xÃ³a máº«u Ã¡o '{tenSP}' do cÃ³ dá»¯ liá»‡u liÃªn káº¿t trong há»‡ thá»‘ng. Gá»£i Ã½: HÃ£y báº¥m nÃºt [Táº¡m áº©n] Ä‘á»ƒ ngá»«ng kinh doanh máº«u Ã¡o nÃ y an toÃ n.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lá»—i khi xÃ³a máº«u Ã¡o: {ex.Message}";
            }

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
                TempData["ErrorMessage"] = "TÃªn danh má»¥c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.";
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            try
            {
                if (model.MaDM.HasValue && model.MaDM.Value > 0)
                {
                    var dm = await _context.DanhMuc.FindAsync(model.MaDM.Value);
                    if (dm == null)
                    {
                        TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y danh má»¥c.";
                        return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
                    }

                    dm.TenDM = model.TenDM.Trim();
                    dm.MoTa = model.MoTa?.Trim();
                    dm.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"ÄÃ£ cáº­p nháº­t danh má»¥c '{dm.TenDM}' thÃ nh cÃ´ng!";
                }
                else
                {
                    bool exists = await _context.DanhMuc.AnyAsync(d => d.TenDM == model.TenDM.Trim());
                    if (exists)
                    {
                        TempData["ErrorMessage"] = $"Danh má»¥c '{model.TenDM}' Ä‘Ã£ tá»“n táº¡i.";
                        return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
                    }

                    var dm = new DanhMuc
                    {
                        TenDM = model.TenDM.Trim(),
                        MoTa = model.MoTa?.Trim(),
                        TrangThai = model.TrangThai
                    };

                    _context.DanhMuc.Add(dm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"ÄÃ£ thÃªm má»›i danh má»¥c '{dm.TenDM}' thÃ nh cÃ´ng!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lá»—i: {ex.Message}";
            }

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


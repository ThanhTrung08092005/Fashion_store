using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            // Đồng bộ hóa giá bán thống nhất cho các biến thể của cùng 1 sản phẩm (nếu dữ liệu cũ bị lệch giá giữa các size)
            var mismatchedProducts = await _context.SanPham
                .Include(s => s.SoLuongSps)
                .Where(s => s.SoLuongSps.Select(v => v.GiaBan).Distinct().Count() > 1)
                .ToListAsync();
            if (mismatchedProducts.Any())
            {
                foreach (var mp in mismatchedProducts)
                {
                    var targetPrice = mp.SoLuongSps.Max(v => v.GiaBan);
                    foreach (var v in mp.SoLuongSps)
                    {
                        v.GiaBan = targetPrice;
                    }
                }
                await _context.SaveChangesAsync();
            }

            var danhMucs = await _context.DanhMuc.Where(d => d.TrangThai).OrderBy(d => d.TenDM).ToListAsync();

            ViewBag.CurrentTab = tab;
            ViewBag.DanhMucList = danhMucs;

            // Thống kê KPI đầu trang
            ViewBag.TotalProducts = await _context.SanPham.CountAsync();
            ViewBag.TotalStock = await _context.SoLuongSp.SumAsync(s => (int?)s.SoLuongTon) ?? 0;
            ViewBag.LowStockCount = await _context.SoLuongSp.CountAsync(s => s.SoLuongTon <= 10);
            ViewBag.TotalCategories = danhMucs.Count;
            ViewBag.NextMaSP = (await _context.SanPham.MaxAsync(s => (int?)s.MaSP) ?? 0) + 1;

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
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            IActionResult Fail(string field, string message)
            {
                if (isAjax) return Json(new { success = false, field, message });
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(ProductManagement));
            }

            string tenSP = model.TenSP?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(tenSP))
                return Fail("TenSP", "Tên sản phẩm áo không được để trống.");
            if (tenSP.Length < 3)
                return Fail("TenSP", "Tên sản phẩm áo phải có ít nhất 3 ký tự.");
            if (tenSP.Length > 200)
                return Fail("TenSP", "Tên sản phẩm áo không được vượt quá 200 ký tự.");

            if (!model.MaDM.HasValue || model.MaDM.Value <= 0)
                return Fail("MaDM", "Vui lòng chọn danh mục áo.");

            string chatLieu = model.ChatLieu?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(chatLieu))
                return Fail("ChatLieu", "Chất liệu vải không được để trống.");
            if (chatLieu.Length < 2)
                return Fail("ChatLieu", "Chất liệu vải phải có ít nhất 2 ký tự.");
            if (chatLieu.Length > 100)
                return Fail("ChatLieu", "Chất liệu vải không được vượt quá 100 ký tự.");

            decimal? unifiedPrice = model.GiaBan
                ?? model.InitialPrice
                ?? model.Variants?.FirstOrDefault(v => v.GiaBan.HasValue && v.GiaBan.Value > 0)?.GiaBan;

            if (!unifiedPrice.HasValue)
                return Fail("GiaBan", "Giá bán niêm yết không được để trống.");
            if (unifiedPrice.Value < 1000 || unifiedPrice.Value > 100000000)
                return Fail("GiaBan", "Giá bán niêm yết phải từ 1.000 đ đến 100.000.000 đ.");
            if (unifiedPrice.Value != Math.Floor(unifiedPrice.Value))
                return Fail("GiaBan", "Giá bán niêm yết phải là số nguyên (VNĐ).");

            string moTa = model.MoTa?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(moTa))
                return Fail("MoTa", "Mô tả sản phẩm không được để trống.");
            if (moTa.Length > 2000)
                return Fail("MoTa", "Mô tả sản phẩm không được vượt quá 2.000 ký tự.");

            var thumbnailFile = model.ThumbnailImageFile ?? model.InitialImageFile;
            var thumbnailPath = string.IsNullOrWhiteSpace(model.HinhAnh) ? null : model.HinhAnh.Trim();
            var detailPath = string.IsNullOrWhiteSpace(model.HinhAnhMoTa) ? null : model.HinhAnhMoTa.Trim();

            if (HasInvalidProductImage(thumbnailFile))
                return Fail("ThumbnailImageFile", "Ảnh thumbnail phải là JPG, PNG hoặc WEBP và không quá 5 MB.");
            if (!IsValidProductImagePath(thumbnailPath))
                return Fail("HinhAnh", "Đường dẫn ảnh thumbnail phải bắt đầu bằng / hoặc http(s)://.");
            if (HasInvalidProductImage(model.DetailImageFile))
                return Fail("DetailImageFile", "Ảnh chi tiết phải là JPG, PNG hoặc WEBP và không quá 5 MB.");
            if (!IsValidProductImagePath(detailPath))
                return Fail("HinhAnhMoTa", "Đường dẫn ảnh chi tiết phải bắt đầu bằng / hoặc http(s)://.");

            try
            {
                if (!await _context.DanhMuc.AnyAsync(category => category.MaDM == model.MaDM.Value && category.TrangThai))
                    return Fail("MaDM", "Danh mục không tồn tại hoặc đang bị khóa.");

                bool duplicateName = await _context.SanPham.AnyAsync(s =>
                    !s.DaXoa &&
                    s.MaSP != (model.MaSP ?? 0) &&
                    s.TenSP.ToLower() == tenSP.ToLower());
                if (duplicateName)
                    return Fail("TenSP", $"Mẫu áo '{tenSP}' đã tồn tại trong hệ thống.");

                SanPham? existingSp = null;
                if (model.MaSP.HasValue && model.MaSP > 0)
                {
                    existingSp = await _context.SanPham.Include(s => s.SoLuongSps).FirstOrDefaultAsync(s => s.MaSP == model.MaSP.Value);
                    if (existingSp == null)
                        return Fail("", "Không tìm thấy sản phẩm cần cập nhật.");
                }

                bool hasThumb = (thumbnailFile is { Length: > 0 }) || !string.IsNullOrWhiteSpace(thumbnailPath) || !string.IsNullOrWhiteSpace(existingSp?.HinhAnh);
                if (!hasThumb)
                    return Fail("HinhAnh", "Ảnh thumbnail / ảnh đại diện không được để trống.");

                bool hasDetail = (model.DetailImageFile is { Length: > 0 }) || !string.IsNullOrWhiteSpace(detailPath) || !string.IsNullOrWhiteSpace(existingSp?.HinhAnhMoTa);
                if (!hasDetail)
                    return Fail("HinhAnhMoTa", "Ảnh chi tiết sản phẩm không được để trống.");

                // Chuẩn bị danh sách biến thể gửi lên (không tự tạo mặc định Size M)
                var submittedVariants = model.Variants ?? new List<ProductVariantItemInput>();

                if (!submittedVariants.Any())
                    return Fail("Variants", "Vui lòng chọn và thêm ít nhất 1 biến thể (Size & Màu) cho sản phẩm.");

                for (int i = 0; i < submittedVariants.Count; i++)
                {
                    var vItem = submittedVariants[i];
                    if (string.IsNullOrWhiteSpace(vItem.KichThuoc))
                        return Fail($"Variants[{i}].KichThuoc", "Vui lòng chọn kích thước (Size) cho biến thể.");
                    if (string.IsNullOrWhiteSpace(vItem.MauSac))
                        return Fail($"Variants[{i}].MauSac", "Vui lòng chọn màu sắc cho biến thể.");
                    if (!vItem.SoLuongTon.HasValue || vItem.SoLuongTon.Value < 0 || vItem.SoLuongTon.Value > 100000)
                        return Fail($"Variants[{i}].SoLuongTon", "Số lượng tồn kho phải từ 0 đến 100.000.");
                }

                // Kiểm tra trùng lặp cặp (Size + Màu sắc) trong danh sách biến thể của cùng sản phẩm
                var duplicateSizeColor = submittedVariants
                    .GroupBy(v => new
                    {
                        Size = v.KichThuoc.Trim().ToUpperInvariant(),
                        Color = v.MauSac.Trim().ToLowerInvariant()
                    })
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicateSizeColor != null)
                {
                    var dupSample = submittedVariants.First(v =>
                        v.KichThuoc.Trim().ToUpperInvariant() == duplicateSizeColor.Key.Size &&
                        v.MauSac.Trim().ToLowerInvariant() == duplicateSizeColor.Key.Color);
                    return Fail("Variants", $"Biến thể Size {dupSample.KichThuoc.Trim().ToUpperInvariant()} - Màu {dupSample.MauSac.Trim()} bị trùng lặp trong danh sách.");
                }

                var uploadedThumbnailPath = await SaveProductImageAsync(thumbnailFile);
                var uploadedDetailPath = await SaveProductImageAsync(model.DetailImageFile);
                var savedThumbnailPath = uploadedThumbnailPath ?? thumbnailPath;
                var savedDetailPath = uploadedDetailPath ?? detailPath;

                if (existingSp != null)
                {
                    existingSp.TenSP = tenSP;
                    existingSp.MaDM = model.MaDM.Value;
                    existingSp.ChatLieu = chatLieu;
                    existingSp.MoTa = moTa;
                    existingSp.HinhAnh = savedThumbnailPath ?? existingSp.HinhAnh;
                    existingSp.HinhAnhMoTa = savedDetailPath ?? existingSp.HinhAnhMoTa;
                    existingSp.TrangThai = model.TrangThai;

                    // Đồng bộ 1 mức giá bán chung & tồn kho cho toàn bộ biến thể
                    for (int i = 0; i < submittedVariants.Count; i++)
                    {
                        var vInput = submittedVariants[i];
                        string sizeUpper = vInput.KichThuoc.Trim().ToUpperInvariant();
                        string colorTrimmed = vInput.MauSac.Trim();

                        if (vInput.MaSL.HasValue && vInput.MaSL.Value > 0)
                        {
                            var dbVariant = existingSp.SoLuongSps.FirstOrDefault(x => x.MaSL == vInput.MaSL.Value);
                            if (dbVariant != null)
                            {
                                dbVariant.KichThuoc = sizeUpper;
                                dbVariant.MauSac = colorTrimmed;
                                dbVariant.SKU = GenerateSynchronizedSku(existingSp.MaSP, sizeUpper, colorTrimmed, dbVariant.MaSL);
                                dbVariant.GiaBan = unifiedPrice.Value;
                                dbVariant.SoLuongTon = vInput.SoLuongTon!.Value;
                                dbVariant.HinhAnh = existingSp.HinhAnh;
                            }
                        }
                        else
                        {
                            string sku = GenerateSynchronizedSku(existingSp.MaSP, sizeUpper, colorTrimmed);

                            _context.SoLuongSp.Add(new SoLuongSp
                            {
                                MaSP = existingSp.MaSP,
                                SKU = sku,
                                KichThuoc = sizeUpper,
                                MauSac = colorTrimmed,
                                GiaBan = unifiedPrice.Value,
                                SoLuongTon = vInput.SoLuongTon!.Value,
                                HinhAnh = existingSp.HinhAnh,
                                TrangThai = true
                            });
                        }
                    }

                    // Đảm bảo mọi biến thể của mẫu áo đều có chung 1 mức giá niêm yết
                    foreach (var v in existingSp.SoLuongSps)
                    {
                        v.GiaBan = unifiedPrice.Value;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật mẫu áo '{existingSp.TenSP}' thành công (Giá niêm yết: {unifiedPrice.Value:N0} đ).";
                    if (isAjax)
                        return Json(new { success = true, message = TempData["SuccessMessage"], redirectUrl = Url.Action(nameof(ProductManagement), new { tab = "SANPHAM" }) });
                }
                else
                {
                    var sp = new SanPham
                    {
                        TenSP = tenSP,
                        MaDM = model.MaDM.Value,
                        ChatLieu = chatLieu,
                        MoTa = moTa,
                        HinhAnh = savedThumbnailPath,
                        HinhAnhMoTa = savedDetailPath,
                        TrangThai = model.TrangThai,
                        NgayTao = DateTime.Now
                    };

                    _context.SanPham.Add(sp);
                    await _context.SaveChangesAsync();

                    for (int i = 0; i < submittedVariants.Count; i++)
                    {
                        var vInput = submittedVariants[i];
                        string sizeUpper = vInput.KichThuoc.Trim().ToUpperInvariant();
                        string colorTrimmed = vInput.MauSac.Trim();
                        string sku = GenerateSynchronizedSku(sp.MaSP, sizeUpper, colorTrimmed);

                        var variant = new SoLuongSp
                        {
                            MaSP = sp.MaSP,
                            SKU = sku,
                            KichThuoc = sizeUpper,
                            MauSac = colorTrimmed,
                            GiaBan = unifiedPrice.Value,
                            SoLuongTon = vInput.SoLuongTon!.Value,
                            HinhAnh = savedThumbnailPath,
                            TrangThai = true
                        };

                        _context.SoLuongSp.Add(variant);
                    }

                    await _context.SaveChangesAsync();

                    var sizeSummary = string.Join(", ", submittedVariants.Select(v => v.KichThuoc.Trim().ToUpperInvariant()).Distinct());
                    TempData["SuccessMessage"] = $"Đã thêm mẫu áo '{sp.TenSP}' thành công ({submittedVariants.Count} biến thể, Size: {sizeSummary}, Giá: {unifiedPrice.Value:N0} đ).";
                    if (isAjax)
                        return Json(new { success = true, message = TempData["SuccessMessage"], redirectUrl = Url.Action(nameof(ProductManagement), new { tab = "SANPHAM" }) });
                }
            }
            catch (Exception ex)
            {
                return Fail("", $"Không thể lưu sản phẩm: {ex.Message}");
            }

            return RedirectToAction(nameof(ProductManagement));
        }

        private static string ToColorSkuCode(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return "MAU";
            string normalized = color.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    char mapped = c switch
                    {
                        'đ' => 'd',
                        'Đ' => 'D',
                        _ => c
                    };
                    if (char.IsLetterOrDigit(mapped))
                    {
                        sb.Append(char.ToUpperInvariant(mapped));
                    }
                }
            }
            return sb.Length > 0 ? sb.ToString() : "MAU";
        }

        private string GenerateSynchronizedSku(int maSP, string kichThuoc, string mauSac, int? excludeMaSL = null)
        {
            string sizeCode = string.IsNullOrWhiteSpace(kichThuoc) ? "SIZE" : kichThuoc.Trim().ToUpperInvariant();
            string colorCode = ToColorSkuCode(mauSac);
            string baseSku = $"SP{maSP}-{sizeCode}-{colorCode}";
            string candidate = baseSku;
            int suffix = 2;

            while (_context.SoLuongSp.Local.Any(s => s.SKU == candidate && s.MaSL != (excludeMaSL ?? 0))
                   || _context.SoLuongSp.Any(s => s.SKU == candidate && s.MaSL != (excludeMaSL ?? 0)))
            {
                candidate = $"{baseSku}-{suffix}";
                suffix++;
            }

            return candidate;
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
            string message = $"Đã ẩn sản phẩm '{product.TenSP}'. Phản hồi và đánh giá được giữ nguyên.";
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

            decimal unifiedGiaBan = sp.SoLuongSps.Any() ? sp.SoLuongSps.Max(v => v.GiaBan) : 0m;

            var result = new
            {
                success = true,
                maSP = sp.MaSP,
                tenSP = sp.TenSP,
                tenDM = sp.DanhMuc?.TenDM ?? "Chưa phân loại",
                chatLieu = sp.ChatLieu ?? "",
                giaBan = unifiedGiaBan,
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
                    giaBan = unifiedGiaBan,
                    soLuongTon = v.SoLuongTon,
                    hinhAnh = v.HinhAnh,
                    trangThai = v.TrangThai
                }).ToList()
            };

            return Json(result);
        }

        // ==========================================
        // 6. THÊM / CẬP NHẬT BIẾN THỂ (SIZE / MÀU)
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
                string sizeUpper = model.KichThuoc.Trim().ToUpperInvariant();
                string colorTrimmed = model.MauSac.Trim();
                int excludeMaSL = model.MaSL ?? 0;

                // Ràng buộc không trùng cặp (Size + Màu sắc) trong cùng 1 sản phẩm
                bool duplicatePair = await _context.SoLuongSp.AnyAsync(s =>
                    s.MaSP == model.MaSP &&
                    s.MaSL != excludeMaSL &&
                    s.KichThuoc.ToUpper() == sizeUpper &&
                    s.MauSac.ToLower() == colorTrimmed.ToLower());

                if (duplicatePair)
                {
                    string dupMsg = $"Biến thể Size {sizeUpper} - Màu {colorTrimmed} đã tồn tại trong mẫu áo này.";
                    if (isAjax) return Json(new { success = false, field = "MauSac", message = dupMsg });
                    TempData["ErrorMessage"] = dupMsg;
                    return RedirectToAction(nameof(ProductManagement));
                }

                // Tự động lấy mức giá chung của sản phẩm (hoặc đồng bộ nếu gửi kèm GiaBan hợp lệ)
                decimal currentUnifiedPrice = await _context.SoLuongSp
                    .Where(s => s.MaSP == model.MaSP)
                    .MaxAsync(s => (decimal?)s.GiaBan) ?? 0m;

                decimal finalPrice = (model.GiaBan.HasValue && model.GiaBan.Value >= 1000)
                    ? model.GiaBan.Value
                    : (currentUnifiedPrice >= 1000 ? currentUnifiedPrice : 100000m);

                if (model.MaSL.HasValue && model.MaSL.Value > 0)
                {
                    var v = await _context.SoLuongSp.FindAsync(model.MaSL.Value);
                    if (v == null)
                    {
                        if (isAjax) return Json(new { success = false, message = "Không tìm thấy biến thể." });
                        TempData["ErrorMessage"] = "Không tìm thấy biến thể.";
                        return RedirectToAction(nameof(ProductManagement));
                    }

                    v.SKU = GenerateSynchronizedSku(v.MaSP, sizeUpper, colorTrimmed, v.MaSL);
                    v.KichThuoc = sizeUpper;
                    v.MauSac = colorTrimmed;
                    v.GiaBan = finalPrice;
                    v.SoLuongTon = model.SoLuongTon;
                    if (!string.IsNullOrWhiteSpace(model.HinhAnh))
                        v.HinhAnh = model.HinhAnh.Trim();
                    v.TrangThai = model.TrangThai;

                    // Đồng bộ giá chung cho toàn bộ các biến thể của sản phẩm
                    var siblings = await _context.SoLuongSp.Where(s => s.MaSP == v.MaSP && s.MaSL != v.MaSL).ToListAsync();
                    foreach (var sib in siblings)
                    {
                        sib.GiaBan = finalPrice;
                    }

                    await _context.SaveChangesAsync();
                    if (isAjax) return Json(new { success = true, message = $"Đã cập nhật biến thể SKU '{v.SKU}' thành công!" });
                    TempData["SuccessMessage"] = $"Đã cập nhật biến thể SKU '{v.SKU}' thành công!";
                }
                else
                {
                    string sku = GenerateSynchronizedSku(model.MaSP, sizeUpper, colorTrimmed);
                    var parentImg = await _context.SanPham.Where(s => s.MaSP == model.MaSP).Select(s => s.HinhAnh).FirstOrDefaultAsync();

                    var v = new SoLuongSp
                    {
                        MaSP = model.MaSP,
                        SKU = sku,
                        KichThuoc = sizeUpper,
                        MauSac = colorTrimmed,
                        GiaBan = finalPrice,
                        SoLuongTon = model.SoLuongTon,
                        HinhAnh = !string.IsNullOrWhiteSpace(model.HinhAnh) ? model.HinhAnh.Trim() : parentImg,
                        TrangThai = true
                    };

                    _context.SoLuongSp.Add(v);

                    // Đồng bộ giá chung cho các biến thể khác cùng sản phẩm
                    var siblings = await _context.SoLuongSp.Where(s => s.MaSP == model.MaSP).ToListAsync();
                    foreach (var sib in siblings)
                    {
                        sib.GiaBan = finalPrice;
                    }

                    await _context.SaveChangesAsync();
                    if (isAjax) return Json(new { success = true, message = $"Đã thêm mới biến thể Size {v.KichThuoc} - Màu {v.MauSac} thành công!" });
                    TempData["SuccessMessage"] = $"Đã thêm mới biến thể Size {v.KichThuoc} - Màu {v.MauSac} thành công!";
                }
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(ProductManagement));
        }

        // ==========================================
        // 7. XÓA BIẾN THỂ SIZE/MÀU
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            var v = await _context.SoLuongSp.FindAsync(id);
            if (v == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy biến thể." });
                TempData["ErrorMessage"] = "Không tìm thấy biến thể.";
                return RedirectToAction(nameof(ProductManagement));
            }

            int totalVariantsOfProduct = await _context.SoLuongSp.CountAsync(s => s.MaSP == v.MaSP);
            if (totalVariantsOfProduct <= 1)
            {
                string minVariantMsg = "Không thể xóa biến thể cuối cùng! Mỗi sản phẩm phải có ít nhất 1 biến thể (Size & Màu).";
                if (isAjax) return Json(new { success = false, message = minVariantMsg });
                TempData["ErrorMessage"] = minVariantMsg;
                return RedirectToAction(nameof(ProductManagement));
            }

            string sku = v.SKU;
            _context.SoLuongSp.Remove(v);
            await _context.SaveChangesAsync();

            if (isAjax) return Json(new { success = true, message = $"Đã xóa biến thể SKU '{sku}' thành công." });
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
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || (Request.Headers.Accept.ToString()?.Contains("application/json") ?? false);

            IActionResult FailCategory(string field, string message)
            {
                if (isAjax) return Json(new { success = false, field, message });
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(ProductManagement), new { tab = "DANHMUC" });
            }

            string categoryName = model.TenDM?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(categoryName))
                return FailCategory("TenDM", "Tên danh mục không được để trống.");
            if (categoryName.Length > 100)
                return FailCategory("TenDM", "Tên danh mục không được vượt quá 100 ký tự.");

            string categoryDesc = model.MoTa?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(categoryDesc))
                return FailCategory("MoTa", "Mô tả danh mục không được để trống.");
            if (categoryDesc.Length > 500)
                return FailCategory("MoTa", "Mô tả danh mục không được vượt quá 500 ký tự.");

            try
            {
                bool duplicateName = await _context.DanhMuc.AnyAsync(category =>
                    category.MaDM != (model.MaDM ?? 0) && category.TenDM.ToLower() == categoryName.ToLower());
                if (duplicateName)
                    return FailCategory("TenDM", $"Danh mục '{categoryName}' đã tồn tại.");

                if (model.MaDM.HasValue && model.MaDM.Value > 0)
                {
                    var dm = await _context.DanhMuc.FindAsync(model.MaDM.Value);
                    if (dm == null)
                        return FailCategory("", "Không tìm thấy danh mục.");

                    dm.TenDM = categoryName;
                    dm.MoTa = categoryDesc;
                    dm.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{dm.TenDM}' thành công.";
                }
                else
                {
                    var dm = new DanhMuc
                    {
                        TenDM = categoryName,
                        MoTa = categoryDesc,
                        TrangThai = model.TrangThai
                    };

                    _context.DanhMuc.Add(dm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã thêm mới danh mục '{dm.TenDM}' thành công.";
                }

                if (isAjax)
                    return Json(new { success = true, message = TempData["SuccessMessage"], redirectUrl = Url.Action(nameof(ProductManagement), new { tab = "DANHMUC" }) });
            }
            catch (Exception ex)
            {
                return FailCategory("", $"Không thể lưu danh mục: {ex.Message}");
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


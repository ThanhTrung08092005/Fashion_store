using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fashion_store.Models;

namespace Fashion_store.ViewModels
{
    // ViewModel hiển thị một dòng sản phẩm áo trên bảng
    public class ProductItemViewModel
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; } = string.Empty;
        public int MaDM { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public string? ChatLieu { get; set; }
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public string? HinhAnhMoTa { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        public List<SoLuongSp> BienThes { get; set; } = new List<SoLuongSp>();

        public int TongTonKho => BienThes.Sum(b => b.SoLuongTon);
        public decimal GiaBan => BienThes.Any() ? BienThes.Max(b => b.GiaBan) : 0;
        public decimal GiaMin => BienThes.Any() ? BienThes.Min(b => b.GiaBan) : 0;
        public decimal GiaMax => BienThes.Any() ? BienThes.Max(b => b.GiaBan) : 0;
        public List<string> KichThuocs => BienThes.Select(b => b.KichThuoc).Distinct().ToList();
        public List<string> MauSacs => BienThes.Select(b => b.MauSac).Distinct().ToList();
        public string? HinhAnhDaiDien => !string.IsNullOrWhiteSpace(HinhAnh)
            ? HinhAnh
            : BienThes.FirstOrDefault(b => !string.IsNullOrEmpty(b.HinhAnh))?.HinhAnh;
    }

    public class ProductVariantItemInput
    {
        public int? MaSL { get; set; }
        public string? SKU { get; set; }
        public string KichThuoc { get; set; } = string.Empty;
        public string MauSac { get; set; } = string.Empty;
        public decimal? GiaBan { get; set; }
        public int? SoLuongTon { get; set; }
    }

    // ViewModel cho Form Thêm / Sửa Sản Phẩm
    public class ProductManageViewModel
    {
        public int? MaSP { get; set; }

        [Display(Name = "Tên áo")]
        public string? TenSP { get; set; }

        [Display(Name = "Danh mục")]
        public int? MaDM { get; set; }

        [Display(Name = "Chất liệu vải")]
        public string? ChatLieu { get; set; }

        [Display(Name = "Giá bán niêm yết (VNĐ)")]
        public decimal? GiaBan { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string? MoTa { get; set; }

        [Display(Name = "Trạng thái kinh doanh")]
        public bool TrangThai { get; set; } = true;

        // Danh sách biến thể (Size / Màu / SKU / Tồn kho)
        public List<ProductVariantItemInput> Variants { get; set; } = new List<ProductVariantItemInput>();

        // Biến thể khởi tạo ban đầu (tương thích ngược)
        [Display(Name = "Mã SKU")]
        public string? InitialSKU { get; set; }

        [Display(Name = "Kích thước")]
        public string? InitialSize { get; set; }

        [Display(Name = "Màu sắc")]
        public string? InitialColor { get; set; }

        [Display(Name = "Giá bán")]
        public decimal? InitialPrice { get; set; }

        [Display(Name = "Số lượng tồn")]
        public int? InitialStock { get; set; }

        [Display(Name = "Đường dẫn hình ảnh")]
        public string? InitialImage { get; set; }

        [Display(Name = "Tệp hình ảnh")]
        public Microsoft.AspNetCore.Http.IFormFile? InitialImageFile { get; set; }

        [Display(Name = "Ảnh thumbnail")]
        public Microsoft.AspNetCore.Http.IFormFile? ThumbnailImageFile { get; set; }

        [Display(Name = "Ảnh chi tiết")]
        public Microsoft.AspNetCore.Http.IFormFile? DetailImageFile { get; set; }

        [StringLength(500)]
        public string? HinhAnh { get; set; }

        [StringLength(500)]
        public string? HinhAnhMoTa { get; set; }
    }

    // ViewModel cho Biến thể (Kích thước, Màu sắc, SKU, Tồn kho)
    public class VariantManageViewModel
    {
        public int? MaSL { get; set; }

        [Required]
        public int MaSP { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã SKU")]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kích thước (Size)")]
        [StringLength(20)]
        public string KichThuoc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập màu sắc")]
        [StringLength(50)]
        public string MauSac { get; set; } = string.Empty;

        public decimal? GiaBan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000")]
        public int SoLuongTon { get; set; }

        [StringLength(500)]
        public string? HinhAnh { get; set; }

        public bool TrangThai { get; set; } = true;
    }

    // ViewModel cho Quản lý Danh mục
    public class CategoryManageViewModel
    {
        public int? MaDM { get; set; }

        public string? TenDM { get; set; }

        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}

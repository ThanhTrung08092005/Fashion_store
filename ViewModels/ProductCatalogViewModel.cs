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
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        public List<SoLuongSp> BienThes { get; set; } = new List<SoLuongSp>();

        public int TongTonKho => BienThes.Sum(b => b.SoLuongTon);
        public decimal GiaMin => BienThes.Any() ? BienThes.Min(b => b.GiaBan) : 0;
        public decimal GiaMax => BienThes.Any() ? BienThes.Max(b => b.GiaBan) : 0;
        public List<string> KichThuocs => BienThes.Select(b => b.KichThuoc).Distinct().ToList();
        public List<string> MauSacs => BienThes.Select(b => b.MauSac).Distinct().ToList();
        public string? HinhAnhDaiDien => BienThes.FirstOrDefault(b => !string.IsNullOrEmpty(b.HinhAnh))?.HinhAnh;
    }

    // ViewModel cho Form Thêm / Sửa Sản Phẩm
    public class ProductManageViewModel
    {
        public int? MaSP { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm áo")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
        [Display(Name = "Tên áo")]
        public string TenSP { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục áo")]
        [Display(Name = "Danh mục")]
        public int MaDM { get; set; }

        [StringLength(100, ErrorMessage = "Chất liệu tối đa 100 ký tự")]
        [Display(Name = "Chất liệu vải")]
        public string? ChatLieu { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string? MoTa { get; set; }

        [Display(Name = "Trạng thái kinh doanh")]
        public bool TrangThai { get; set; } = true;

        // Biến thể khởi tạo ban đầu (khi tạo mới)
        [Display(Name = "Mã SKU")]
        public string? InitialSKU { get; set; }

        [Display(Name = "Kích thước")]
        public string? InitialSize { get; set; } = "M";

        [Display(Name = "Màu sắc")]
        public string? InitialColor { get; set; } = "Đen";

        [Display(Name = "Giá bán")]
        public decimal? InitialPrice { get; set; } = 250000;

        [Display(Name = "Số lượng tồn")]
        public int? InitialStock { get; set; } = 50;

        [Display(Name = "Đường dẫn hình ảnh")]
        public string? InitialImage { get; set; }
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

        [Required(ErrorMessage = "Vui lòng nhập kích thước (Size)")]
        [StringLength(20)]
        public string KichThuoc { get; set; } = "M";

        [Required(ErrorMessage = "Vui lòng nhập màu sắc")]
        [StringLength(50)]
        public string MauSac { get; set; } = "Đen";

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(0, 100000000, ErrorMessage = "Giá bán không hợp lệ")]
        public decimal GiaBan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, 100000, ErrorMessage = "Số lượng tồn không hợp lệ")]
        public int SoLuongTon { get; set; }

        [StringLength(500)]
        public string? HinhAnh { get; set; }

        public bool TrangThai { get; set; } = true;
    }

    // ViewModel cho Quản lý Danh mục
    public class CategoryManageViewModel
    {
        public int? MaDM { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
        public string TenDM { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}

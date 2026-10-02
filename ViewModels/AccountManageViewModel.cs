using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fashion_store.Models;

namespace Fashion_store.ViewModels
{
    // Items hiển thị trong bảng Quản lý Tài khoản CRM
    public class AccountItemViewModel
    {
        public int MaTK { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public int MaVaiTro { get; set; }
        public string TenVaiTro { get; set; } = string.Empty;
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public string UserType { get; set; } = "QUANLY"; // "QUANLY" hoặc "KHACHHANG"
        
        // Thông tin Quản lý (Admin / Quản lý CRM)
        public int? MaQL { get; set; }
        public string? ChucVu { get; set; }

        // Thông tin Khách hàng (CRM)
        public int? MaKH { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public string? SoThich { get; set; }
    }

    // Modal / Form Đăng ký cho Khách hàng (Tab ĐĂNG KÝ trên UI Fashion_store)
    public class RegisterCustomerViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string XacNhanMatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }
    }

    // Modal / Form Thêm Khách Hàng Mới (Cho Admin CRM)
    public class CreateCustomerViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? SoDienThoai { get; set; }

        public DateTime? NgaySinh { get; set; }

        public string? GioiTinh { get; set; }

        public string? DiaChi { get; set; }

        public string? SoThich { get; set; }

        public bool TrangThai { get; set; } = true;
    }

    // Form Tạo Tài khoản Quản lý / Staff CRM
    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public int MaVaiTro { get; set; }

        public string? ChucVu { get; set; } = "Quản lý CRM";

        public bool TrangThai { get; set; } = true;
    }

    // Modal Sửa Tài Khoản / Khách Hàng (CRM)
    public class EditAccountViewModel
    {
        public int MaTK { get; set; }
        public string UserType { get; set; } = "QUANLY"; // "QUANLY" / "KHACHHANG"
        public int? MaQL { get; set; }
        public int? MaKH { get; set; }

        public string TenDangNhap { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? SoDienThoai { get; set; }

        public int MaVaiTro { get; set; }

        public string? ChucVu { get; set; }

        // CRM Khách hàng Info
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public string? SoThich { get; set; }

        public bool TrangThai { get; set; }
    }
}

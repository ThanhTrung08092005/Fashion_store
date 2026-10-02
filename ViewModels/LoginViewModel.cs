using System.ComponentModel.DataAnnotations;

namespace Fashion_store.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập hoặc Email")]
        [Display(Name = "Tên đăng nhập / Email")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }
}

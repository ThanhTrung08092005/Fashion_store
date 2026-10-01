using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FashionCRM.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace FashionCRM.API.Helpers;

public class JwtHelper
{
    private readonly IConfiguration _config;

    public JwtHelper(IConfiguration config) => _config = config;

    public string GenerateToken(TaiKhoan tk, string hoTen, string? email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpireHours"] ?? "8"));
        var role = NormalizeRoleName(tk.VaiTro?.TenVaiTro ?? string.Empty);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tk.MaTK.ToString()),
            new Claim("maTK", tk.MaTK.ToString()),
            new Claim("tenDangNhap", tk.TenDangNhap),
            new Claim("hoTen", hoTen),
            new Claim("vaiTro", role),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string NormalizeRoleName(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return "KhachHang";

        var normalized = roleName.Trim();
        if (normalized.Equals("Admin", StringComparison.OrdinalIgnoreCase) || normalized.Contains("Quản trị", StringComparison.OrdinalIgnoreCase))
            return "Admin";

        if (normalized.Equals("QuanLy", StringComparison.OrdinalIgnoreCase) || normalized.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) || normalized.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            return "QuanLy";

        return "KhachHang";
    }
}

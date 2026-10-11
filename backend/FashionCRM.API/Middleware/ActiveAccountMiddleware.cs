using System.Text.Json;
using FashionCRM.API.Data;
using FashionCRM.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Middleware;

public sealed class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveAccountMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idValue = context.User.FindFirst("maTK")?.Value;
            var hasId = int.TryParse(idValue, out var accountId);
            var account = hasId
                ? await db.TaiKhoans.AsNoTracking()
                    .Include(item => item.KhachHang)
                    .Include(item => item.QuanLy)
                    .FirstOrDefaultAsync(item => item.MaTK == accountId)
                : null;

            var tokenRole = context.User.FindFirst("vaiTro")?.Value;
            var accountRole = account?.MaVaiTro switch
            {
                1 => "QuanLy",
                2 => "KhachHang",
                3 => "Admin",
                _ => string.Empty
            };
            var profileIsActive = account is not null && (account.MaVaiTro switch
            {
                1 => account.QuanLy?.TrangThai == true,
                2 => account.KhachHang is { DaXoa: false },
                3 => true,
                _ => false
            });

            if (account is null || !account.TrangThai || !profileIsActive
                || !string.Equals(tokenRole, accountRole, StringComparison.Ordinal))
            {
                var message = account is null || !account.TrangThai
                    || account.MaVaiTro == 1 && account.QuanLy?.TrangThai == false
                    || account.MaVaiTro == 2 && account.KhachHang?.DaXoa == true
                    ? "Tài khoản đã bị khóa hoặc không còn hoạt động."
                    : "Quyền tài khoản đã thay đổi. Vui lòng đăng nhập lại.";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(ApiResponse<object>.Fail(message)));
                return;
            }
        }

        await _next(context);
    }
}

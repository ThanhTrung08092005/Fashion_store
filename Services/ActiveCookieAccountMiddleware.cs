using System.Security.Claims;
using System.Text.Json;
using Fashion_store.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Fashion_store.Services;

public sealed class ActiveCookieAccountMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveCookieAccountMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, FashionStoreDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasId = int.TryParse(idValue, out var accountId);
            var account = hasId
                ? await db.TaiKhoan.AsNoTracking()
                    .Include(item => item.QuanLy)
                    .Include(item => item.KhachHang)
                    .FirstOrDefaultAsync(item => item.MaTK == accountId)
                : null;

            var roleClaim = context.User.FindFirstValue("MaVaiTro");
            var roleMatches = int.TryParse(roleClaim, out var claimedRole)
                && account?.MaVaiTro == claimedRole;
            var profileIsActive = account is not null && (account.MaVaiTro switch
            {
                1 => account.QuanLy?.TrangThai == true,
                2 => account.KhachHang is { DaXoa: false },
                3 => true,
                _ => false
            });

            if (account is null || !account.TrangThai || !profileIsActive || !roleMatches)
            {
                var isLocked = account is not null
                    && (!account.TrangThai
                        || account.MaVaiTro == 1 && account.QuanLy?.TrangThai == false
                        || account.MaVaiTro == 2 && account.KhachHang?.DaXoa == true);
                var message = isLocked
                    ? "Tài khoản đã bị khóa hoặc không còn hoạt động."
                    : "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
                var isStaff = account?.MaVaiTro is 1 or 3
                    || context.User.IsInRole("Quản lý")
                    || context.User.IsInRole("Admin");

                context.Session.Clear();
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.User = new ClaimsPrincipal(new ClaimsIdentity());

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message }));
                    return;
                }

                var loginPath = isStaff ? "/Admin/Account/Login" : "/Account/Login";
                var query = isLocked ? "?locked=true" : string.Empty;
                context.Response.Redirect(loginPath + query);
                return;
            }
        }

        await _next(context);
    }
}

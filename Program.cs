using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Đăng ký Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Fashion Store API",
        Version = "v1",
        Description = "Tài liệu API cho Đồ án Hệ thống thông tin doanh nghiệp - Fashion Store"
    });

    // Mẹo xử lý dự án MVC: Tránh lỗi trùng route nếu có các Action MVC trùng tên
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình EF Core SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.\\SQLEXPRESS;Database=Fashion_store;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<FashionStoreDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cấu hình Authentication & Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();
// Bật giao diện Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fashion Store API v1");
    c.RoutePrefix = "swagger"; // Đường dẫn truy cập sẽ là: /swagger
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Route cho Admin Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Login}/{id?}");

// Route cho các controller phía storefront như CustomerSurveyController.
// Không đặt mặc định area=Admin ở đây để các đường dẫn /CustomerSurvey/... không bị lệch Area.
app.MapControllerRoute(
    name: "storefront",
    pattern: "{controller}/{action=Index}/{id?}");

// Route Mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}",
    defaults: new { area = "Admin" });

app.Run();

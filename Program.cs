using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Fashion_store.Data;
using Fashion_store.Services;

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
var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<FashionStoreDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddHostedService<SurveyExpirationService>();

// Cấu hình Authentication & Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Phân quyền theo chức vụ (Mô hình 2). Admin được bypass trong Permissions.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissions.PolicyCustomersView, policy =>
        policy.RequireAssertion(context => Permissions.CanViewCustomers(context.User)));

    options.AddPolicy(Permissions.PolicyCustomersManage, policy =>
        policy.RequireAssertion(context => Permissions.CanManageCustomers(context.User)));

    options.AddPolicy(Permissions.PolicyCustomersDelete, policy =>
        policy.RequireAssertion(context => Permissions.CanDeleteCustomers(context.User)));

    options.AddPolicy(Permissions.PolicyProductsView, policy =>
        policy.RequireAssertion(context => Permissions.CanViewProducts(context.User)));

    options.AddPolicy(Permissions.PolicyProductsManage, policy =>
        policy.RequireAssertion(context => Permissions.CanManageProducts(context.User)));

    options.AddPolicy(Permissions.PolicyFeedbackManage, policy =>
        policy.RequireAssertion(context => Permissions.CanManageFeedback(context.User)));
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
var frontendPath = Path.Combine(app.Environment.ContentRootPath, "frontend");
if (Directory.Exists(frontendPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendPath),
        RequestPath = "/frontend"
    });
}

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseMiddleware<ActiveCookieAccountMiddleware>();
app.UseAuthorization();

// Route cho Admin Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Index}/{id?}");

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

EnsurePantsCatalogSeeded(app.Services);

app.Run();

static void EnsurePantsCatalogSeeded(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FashionStoreDbContext>();
        var pantsCategories = new (string Name, string Desc, decimal Price, (string TenSP, string ChatLieu, string MoTa, string Thumb, string Detail)[] Products)[]
        {
            (
                "Quần Jeans Nam",
                "Quần bò denim co giãn nhẹ, phom Slim-Fit và Straight tôn dáng",
                420000m,
                new[]
                {
                    ("Quần Jeans Nam Slim-Fit Co Giãn Indigo", "Denim Cotton 98% pha Spandex", "Quần jeans phom ôm vừa vặn tôn dáng, chất vải denim mềm co giãn thoải mái", "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1604176354204-9268737828e4?auto=format&fit=crop&w=800&q=80"),
                    ("Quần Jeans Nam Ống Đứng Straight-Fit Washed", "Denim 12oz xử lý Wash mềm", "Thiết kế ống đứng cổ điển lịch sự, dễ dàng phối cùng áo thun và sơ mi", "https://images.unsplash.com/photo-1604176354204-9268737828e4?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=800&q=80")
                }
            ),
            (
                "Quần Tây Nam Công Sở",
                "Quần âu phom đứng lịch lãm, vải chống nhăn cao cấp",
                450000m,
                new[]
                {
                    ("Quần Tây Nam Công Sở Phom Regular Chống Nhăn", "Vải Tuyết Mưa pha Rayon cao cấp", "Quần âu phom đứng chuẩn công sở, cạp chun ẩn co giãn linh hoạt và giữ ly tốt", "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?auto=format&fit=crop&w=800&q=80"),
                    ("Quần Âu Nam Sidetab Không Đỉa Lịch Lãm", "Vải Wool pha Polyester thoáng khí", "Thiết kế khóa điều chỉnh hông Sidetab sang trọng chuẩn quý ông hiện đại", "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?auto=format&fit=crop&w=800&q=80")
                }
            ),
            (
                "Quần Kaki & Short Nam",
                "Quần kaki và quần short thoáng mát, năng động dạo phố",
                350000m,
                new[]
                {
                    ("Quần Kaki Nam Dài Chinos Co Giãn Năng Động", "Cotton Khaki dệt chéo co giãn", "Quần kaki Chinos trẻ trung, thấm hút mồ hôi tốt phù hợp mặc đi làm lẫn dạo phố", "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1517445312882-bc9910d016b7?auto=format&fit=crop&w=800&q=80"),
                    ("Quần Short Kaki Nam Ngang Gối Casual Hè", "Cotton pha Linen thoáng mát", "Quần short nam phom suông gọn gàng, cạp phối dây rút tiện lợi cho ngày hè", "https://images.unsplash.com/photo-1517445312882-bc9910d016b7?auto=format&fit=crop&w=800&q=80", "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?auto=format&fit=crop&w=800&q=80")
                }
            )
        };

        foreach (var catInfo in pantsCategories)
        {
            var cat = db.DanhMuc.FirstOrDefault(d => d.TenDM == catInfo.Name);
            if (cat == null)
            {
                cat = new Fashion_store.Models.DanhMuc
                {
                    TenDM = catInfo.Name,
                    MoTa = catInfo.Desc,
                    TrangThai = true
                };
                db.DanhMuc.Add(cat);
                db.SaveChanges();
            }

            foreach (var pInfo in catInfo.Products)
            {
                if (!db.SanPham.Any(s => s.TenSP == pInfo.TenSP))
                {
                    var sp = new Fashion_store.Models.SanPham
                    {
                        MaDM = cat.MaDM,
                        TenSP = pInfo.TenSP,
                        ChatLieu = pInfo.ChatLieu,
                        MoTa = pInfo.MoTa,
                        HinhAnh = pInfo.Thumb,
                        HinhAnhMoTa = pInfo.Detail,
                        TrangThai = true,
                        DaXoa = false,
                        NgayTao = DateTime.Now
                    };
                    db.SanPham.Add(sp);
                    db.SaveChanges();

                    var defaultVariants = new (string Size, string Color, string ColorCode)[]
                    {
                        ("M", "Đen", "DEN"),
                        ("L", "Trắng", "TRANG"),
                        ("XL", "Xanh Navy", "XANHNAVY")
                    };
                    foreach (var v in defaultVariants)
                    {
                        string sku = $"SP{sp.MaSP}-{v.Size}-{v.ColorCode}";
                        if (!db.SoLuongSp.Any(x => x.SKU == sku))
                        {
                            db.SoLuongSp.Add(new Fashion_store.Models.SoLuongSp
                            {
                                MaSP = sp.MaSP,
                                SKU = sku,
                                KichThuoc = v.Size,
                                MauSac = v.Color,
                                GiaBan = catInfo.Price,
                                SoLuongTon = 50,
                                HinhAnh = sp.HinhAnh,
                                TrangThai = true
                            });
                        }
                    }
                    db.SaveChanges();
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seed] Bo qua khoi tao danh muc quan: {ex.Message}");
    }
}

static string ResolveConnectionString(IConfiguration config)
{
    var machineName = Environment.MachineName;

    // 1. Kiem tra cau hinh rieng theo ten may (Connection_TENMAY)
    var machineConn = config.GetConnectionString($"Connection_{machineName}");
    if (!string.IsNullOrWhiteSpace(machineConn))
    {
        Console.WriteLine($"[Database] Tu dong nhan dien may ({machineName}): Su dung Connection_{machineName}");
        return machineConn;
    }

    // 2. Danh sach cac cau hinh du phong / tu dong do
    var candidates = new List<string>();
    var defaultConn = config.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(defaultConn)) candidates.Add(defaultConn);

    candidates.Add(@"Server=.\SQLEXPRESS;Database=Fashion_store;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;");
    candidates.Add(@"Server=.;Database=Fashion_store;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;");
    candidates.Add(@"Server=localhost;Database=Fashion_store;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;");
    candidates.Add(@"Server=LAPTOP-GSU7LU5K\MSSQLSERVER01;Database=Fashion_store;User Id=sa;Password=Bibi@0809;TrustServerCertificate=True;");

    foreach (var connStr in candidates.Distinct())
    {
        try
        {
            var testBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr) { ConnectTimeout = 2 };
            using var testConn = new Microsoft.Data.SqlClient.SqlConnection(testBuilder.ConnectionString);
            testConn.Open();
            Console.WriteLine($"[Database] Tu dong ket noi thanh cong toi SQL Server: {testConn.DataSource} (May: {machineName})");
            return connStr;
        }
        catch { }
    }

    return defaultConn ?? candidates.First();
}


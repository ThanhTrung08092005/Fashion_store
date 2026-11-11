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

        EnsureSampleClosedSurveySeeded(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seed] Bo qua khoi tao danh muc quan: {ex.Message}");
    }
}

static void EnsureSampleClosedSurveySeeded(FashionStoreDbContext db)
{
    var poloSurvey = db.KhaoSat
        .Include(k => k.CauHoiKhaoSats)
            .ThenInclude(c => c.LuaChonCauHois)
        .Include(k => k.PhieuKhaoSats)
            .ThenInclude(p => p.TraLoiKhaoSats)
        .FirstOrDefault(k => k.TenKhaoSat.Contains("Áo Polo Nam Dệt Tổ Ong"));

    if (poloSurvey == null) return;

    bool hasFiveCompletedWithAnswers = poloSurvey.PhieuKhaoSats.Count(p => p.TrangThai == "Đã hoàn thành") == 5
        && poloSurvey.PhieuKhaoSats.SelectMany(p => p.TraLoiKhaoSats).Any();

    if (!poloSurvey.TrangThai && hasFiveCompletedWithAnswers && poloSurvey.CauHoiKhaoSats.Count >= 5)
        return;

    poloSurvey.TrangThai = false;
    poloSurvey.NgayBatDau = new DateTime(2026, 9, 20, 0, 0, 0);
    poloSurvey.NgayKetThuc = new DateTime(2026, 10, 31, 23, 59, 0);

    // Đảm bảo có đủ 5 câu hỏi (Một lựa chọn, Nhiều lựa chọn, Đánh giá sao, Tự luận)
    if (!poloSurvey.CauHoiKhaoSats.Any(c => c.LoaiCauHoi == "Nhiều lựa chọn"))
    {
        var qMulti = new Fashion_store.Models.CauHoiKhaoSat
        {
            MaKS = poloSurvey.MaKS,
            NoiDung = "Màu sắc nào bạn mong muốn có trong bộ sưu tập Áo Polo Thu Đông mới?",
            LoaiCauHoi = "Nhiều lựa chọn",
            ThuTu = 3,
            BatBuoc = true
        };
        db.CauHoiKhaoSat.Add(qMulti);
        db.SaveChanges();

        db.LuaChonCauHoi.AddRange(
            new Fashion_store.Models.LuaChonCauHoi { MaCauHoi = qMulti.MaCauHoi, NoiDung = "Đen cổ điển (Classic Black)", ThuTu = 1 },
            new Fashion_store.Models.LuaChonCauHoi { MaCauHoi = qMulti.MaCauHoi, NoiDung = "Trắng kem (Off-White)", ThuTu = 2 },
            new Fashion_store.Models.LuaChonCauHoi { MaCauHoi = qMulti.MaCauHoi, NoiDung = "Xanh Navy lịch lãm", ThuTu = 3 },
            new Fashion_store.Models.LuaChonCauHoi { MaCauHoi = qMulti.MaCauHoi, NoiDung = "Xanh Rêu / Nâu Cafe Thu Đông", ThuTu = 4 }
        );
        db.SaveChanges();
    }

    if (!poloSurvey.CauHoiKhaoSats.Any(c => c.LoaiCauHoi == "Đánh giá sao"))
    {
        db.CauHoiKhaoSat.Add(new Fashion_store.Models.CauHoiKhaoSat
        {
            MaKS = poloSurvey.MaKS,
            NoiDung = "Bạn đánh giá mấy sao cho thiết kế cổ dệt chống quăn mép của mẫu áo mới?",
            LoaiCauHoi = "Đánh giá sao",
            ThuTu = 4,
            BatBuoc = true
        });
        db.SaveChanges();
    }

    if (!poloSurvey.CauHoiKhaoSats.Any(c => c.LoaiCauHoi == "Tự luận"))
    {
        db.CauHoiKhaoSat.Add(new Fashion_store.Models.CauHoiKhaoSat
        {
            MaKS = poloSurvey.MaKS,
            NoiDung = "Bạn có góp ý thêm gì về form dáng hoặc chi tiết thiết kế của mẫu áo Polo mới không?",
            LoaiCauHoi = "Tự luận",
            ThuTu = 5,
            BatBuoc = false
        });
        db.SaveChanges();
    }

    // Nạp lại danh sách câu hỏi và lựa chọn
    var questions = db.CauHoiKhaoSat
        .Include(c => c.LuaChonCauHois)
        .Where(c => c.MaKS == poloSurvey.MaKS)
        .OrderBy(c => c.ThuTu)
        .ToList();

    var customerIds = db.KhachHang
        .Where(k => !k.DaXoa)
        .OrderBy(k => k.MaKH)
        .Select(k => k.MaKH)
        .Take(5)
        .ToList();

    if (customerIds.Count < 5) return;

    // Xóa các câu trả lời và phiếu cũ của khảo sát này để tạo chuẩn 5 phiếu đã hoàn thành
    var existingTickets = db.PhieuKhaoSat.Where(p => p.MaKS == poloSurvey.MaKS).ToList();
    var existingTicketIds = existingTickets.Select(p => p.MaPhieuKS).ToList();
    var existingAnswers = db.TraLoiKhaoSat.Where(a => existingTicketIds.Contains(a.MaPhieuKS)).ToList();
    if (existingAnswers.Any()) db.TraLoiKhaoSat.RemoveRange(existingAnswers);
    if (existingTickets.Any()) db.PhieuKhaoSat.RemoveRange(existingTickets);
    db.SaveChanges();

    var q1 = questions.FirstOrDefault(q => q.ThuTu == 1);
    var q2 = questions.FirstOrDefault(q => q.ThuTu == 2);
    var q3 = questions.FirstOrDefault(q => q.LoaiCauHoi == "Nhiều lựa chọn");
    var q4 = questions.FirstOrDefault(q => q.LoaiCauHoi == "Đánh giá sao");
    var q5 = questions.FirstOrDefault(q => q.LoaiCauHoi == "Tự luận");

    var q1Opts = q1?.LuaChonCauHois.OrderBy(o => o.ThuTu).ToList() ?? new();
    var q2Opts = q2?.LuaChonCauHois.OrderBy(o => o.ThuTu).ToList() ?? new();
    var q3Opts = q3?.LuaChonCauHois.OrderBy(o => o.ThuTu).ToList() ?? new();

    var feedbacks = new[]
    {
        "Nên giữ form Regular vừa vặn, không quá bó ở phần bắp tay để dễ vận động.",
        "Mong cửa hàng ra thêm màu xanh rêu đậm và thêu logo nhỏ tinh tế ở ngực trái.",
        "Chất vải tổ ong thoáng mát rất hợp thời tiết Sài Gòn, nên có thêm size XXL.",
        "Cổ áo nên làm khóa kéo zip hoặc cúc ẩn sẽ trẻ trung và lịch sự hơn khi đi làm.",
        "Rất hài lòng với dòng Polo của shop, chỉ cần giữ độ bền màu sau khi giặt máy."
    };
    var stars = new[] { 5, 4, 5, 4, 5 };
    var q1Picks = new[] { 0, 0, 1, 2, 0 };
    var q2Picks = new[] { 1, 1, 0, 1, 2 };
    var q3Picks = new[]
    {
        new[] { 0, 2 },
        new[] { 1, 2 },
        new[] { 0, 3 },
        new[] { 0, 1, 2 },
        new[] { 2, 3 }
    };

    for (int i = 0; i < 5; i++)
    {
        var submitTime = new DateTime(2026, 9, 22 + i, 10 + i, 15, 0);
        var ticket = new Fashion_store.Models.PhieuKhaoSat
        {
            MaKS = poloSurvey.MaKS,
            MaKH = customerIds[i],
            NgayBatDau = submitTime.AddDays(-1),
            NgayNop = submitTime,
            TrangThai = "Đã hoàn thành"
        };
        db.PhieuKhaoSat.Add(ticket);
        db.SaveChanges();

        if (q1 != null && q1Opts.Count > q1Picks[i])
        {
            db.TraLoiKhaoSat.Add(new Fashion_store.Models.TraLoiKhaoSat
            {
                MaPhieuKS = ticket.MaPhieuKS,
                MaCauHoi = q1.MaCauHoi,
                MaLuaChon = q1Opts[q1Picks[i]].MaLuaChon,
                NgayNop = submitTime
            });
        }

        if (q2 != null && q2Opts.Count > q2Picks[i])
        {
            db.TraLoiKhaoSat.Add(new Fashion_store.Models.TraLoiKhaoSat
            {
                MaPhieuKS = ticket.MaPhieuKS,
                MaCauHoi = q2.MaCauHoi,
                MaLuaChon = q2Opts[q2Picks[i]].MaLuaChon,
                NgayNop = submitTime
            });
        }

        if (q3 != null && q3Opts.Any())
        {
            foreach (var pickIdx in q3Picks[i].Where(idx => idx < q3Opts.Count))
            {
                db.TraLoiKhaoSat.Add(new Fashion_store.Models.TraLoiKhaoSat
                {
                    MaPhieuKS = ticket.MaPhieuKS,
                    MaCauHoi = q3.MaCauHoi,
                    MaLuaChon = q3Opts[pickIdx].MaLuaChon,
                    NgayNop = submitTime
                });
            }
        }

        if (q4 != null)
        {
            db.TraLoiKhaoSat.Add(new Fashion_store.Models.TraLoiKhaoSat
            {
                MaPhieuKS = ticket.MaPhieuKS,
                MaCauHoi = q4.MaCauHoi,
                SoSao = stars[i],
                NgayNop = submitTime
            });
        }

        if (q5 != null)
        {
            db.TraLoiKhaoSat.Add(new Fashion_store.Models.TraLoiKhaoSat
            {
                MaPhieuKS = ticket.MaPhieuKS,
                MaCauHoi = q5.MaCauHoi,
                NoiDungTraLoi = feedbacks[i],
                NgayNop = submitTime
            });
        }

        db.SaveChanges();
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


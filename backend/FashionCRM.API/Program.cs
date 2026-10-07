using System.Text;
using FashionCRM.API.Data;
using FashionCRM.API.Helpers;
using FashionCRM.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FashionCRM.API.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var dbConnectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(dbConnectionString));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5500" };

builder.Services.AddCors(opts =>
    opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Fashion CRM API",
        Version     = "v1",
        Description = "API cho hệ thống CRM cửa hàng thời trang"
    });

    // JWT in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Nhập token JWT: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fashion CRM API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Ensure database exists and seed only once when empty ───────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Không xóa database mỗi lần chạy. Chỉ tạo nếu chưa có và seed tối thiểu 1 lần.
        db.Database.EnsureCreated();
        DbInitializer.Seed(db);
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "Database initialization failed. Check the SQL Server connection and schema.");
    }
}

app.Run();

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

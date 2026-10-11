using FashionCRM.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Data;

/// <summary>
/// Chuẩn hóa dữ liệu khởi tạo theo schema mới của Fashion_store.
/// </summary>
public static class DbInitializer
{
    public static void Seed(AppDbContext db)
    {
        if (!db.VaiTros.Any())
            db.VaiTros.AddRange(
                new VaiTro { TenVaiTro = "Quản lý", MoTa = "Nhân viên bán hàng và quản lý CRM" },
                new VaiTro { TenVaiTro = "Khách hàng", MoTa = "Khách hàng mua sắm thời trang nam" },
                new VaiTro { TenVaiTro = "Admin", MoTa = "Quản trị viên có toàn quyền hệ thống" }
            );
        else if (!db.VaiTros.Any(role => role.MaVaiTro == 3))
            db.VaiTros.Add(new VaiTro { TenVaiTro = "Admin", MoTa = "Quản trị viên có toàn quyền hệ thống" });

        db.SaveChanges();

        var adminRoleId = db.VaiTros.Where(role => role.TenVaiTro == "Admin").Select(role => role.MaVaiTro).FirstOrDefault();
        var seededAdmin = db.TaiKhoans.FirstOrDefault(account => account.TenDangNhap == "admin");
        if (seededAdmin != null && adminRoleId > 0 && seededAdmin.MaVaiTro != adminRoleId)
        {
            seededAdmin.MaVaiTro = adminRoleId;
            db.SaveChanges();
        }

        if (!db.TaiKhoans.Any())
        {
            db.TaiKhoans.AddRange(
                new TaiKhoan { TenDangNhap = "admin", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = adminRoleId, TrangThai = true, NgayTao = DateTime.Now },
                new TaiKhoan { TenDangNhap = "manager01", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 1, TrangThai = true, NgayTao = DateTime.Now },
                new TaiKhoan { TenDangNhap = "customer1", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 2, TrangThai = true, NgayTao = DateTime.Now }
            );
            db.SaveChanges();
        }

        var admin = db.TaiKhoans.FirstOrDefault(account => account.TenDangNhap == "admin");
        if (admin != null && !db.QuanLys.Any(manager => manager.MaTK == admin.MaTK))
            db.QuanLys.Add(new QuanLy
            {
                MaTK = admin.MaTK,
                HoTen = "Quản trị viên hệ thống",
                Email = "admin@fashionstore.com",
                SoDienThoai = "0901234567",
                ChucVu = "Admin",
                TrangThai = true
            });

        var managerAccount = db.TaiKhoans.FirstOrDefault(account => account.TenDangNhap == "manager01");
        if (managerAccount != null && !db.QuanLys.Any(manager => manager.MaTK == managerAccount.MaTK))
            db.QuanLys.Add(new QuanLy
        {
            MaTK = managerAccount.MaTK,
            HoTen = "Trần Đức Long",
            Email = "manager@fashionstore.com",
            SoDienThoai = "0901234001",
            ChucVu = "Quản lý cửa hàng",
            TrangThai = true
        });

        var customerAccount = db.TaiKhoans.FirstOrDefault(account => account.TenDangNhap == "customer1");
        if (customerAccount != null && !db.KhachHangs.Any(customer => customer.MaTK == customerAccount.MaTK))
            db.KhachHangs.Add(new KhachHang
        {
            MaTK = customerAccount.MaTK,
            HoTen = "Nguyễn Văn An",
            Email = "an.nguyen@gmail.com",
            SoDienThoai = "0912345678",
            NgaySinh = new DateOnly(1998, 5, 15),
            GioiTinh = "Nam",
            DiaChi = "Số 45 Cầu Giấy, Hà Nội",
            NgayDangKy = DateTime.Now,
            DaXoa = false
            });

        db.SaveChanges();
    }
}

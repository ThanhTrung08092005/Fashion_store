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
        if (db.VaiTros.Any()) return;

        db.VaiTros.AddRange(
            new VaiTro { MaVaiTro = 1, TenVaiTro = "Quản lý", MoTa = "Quản trị viên / Quản lý hệ thống" },
            new VaiTro { MaVaiTro = 2, TenVaiTro = "Khách hàng", MoTa = "Khách hàng mua sắm thời trang nam" }
        );

        db.TaiKhoans.AddRange(
            new TaiKhoan { MaTK = 1, TenDangNhap = "admin", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 1, TrangThai = true, NgayTao = DateTime.Now },
            new TaiKhoan { MaTK = 2, TenDangNhap = "manager01", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 1, TrangThai = true, NgayTao = DateTime.Now },
            new TaiKhoan { MaTK = 3, TenDangNhap = "customer1", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 2, TrangThai = true, NgayTao = DateTime.Now }
        );

        db.QuanLys.Add(new QuanLy
        {
            MaQL = 1,
            MaTK = 2,
            HoTen = "Trần Đức Long",
            Email = "manager@fashionstore.com",
            SoDienThoai = "0901234001",
            ChucVu = "Quản lý cửa hàng",
            TrangThai = true
        });

        db.KhachHangs.Add(new KhachHang
        {
            MaKH = 1,
            MaTK = 3,
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

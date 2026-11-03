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
                ChucVu = "Quản lý Cửa Hàng",
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
            var cat = db.DanhMucs.FirstOrDefault(d => d.TenDM == catInfo.Name);
            if (cat == null)
            {
                cat = new DanhMuc
                {
                    TenDM = catInfo.Name,
                    MoTa = catInfo.Desc,
                    TrangThai = true
                };
                db.DanhMucs.Add(cat);
                db.SaveChanges();
            }

            foreach (var pInfo in catInfo.Products)
            {
                if (!db.SanPhams.Any(s => s.TenSP == pInfo.TenSP))
                {
                    var sp = new SanPham
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
                    db.SanPhams.Add(sp);
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
                        if (!db.SoLuongSps.Any(x => x.SKU == sku))
                        {
                            db.SoLuongSps.Add(new SoLuongSp
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
}

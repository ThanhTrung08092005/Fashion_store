using FashionCRM.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionCRM.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VaiTro> VaiTros { get; set; }
    public DbSet<TaiKhoan> TaiKhoans { get; set; }
    public DbSet<KhachHang> KhachHangs { get; set; }
    public DbSet<QuanLy> QuanLys { get; set; }
    public DbSet<DanhMuc> DanhMucs { get; set; }
    public DbSet<KhachHangSoThich> KhachHangSoThiches { get; set; }
    public DbSet<SanPham> SanPhams { get; set; }
    public DbSet<SoLuongSp> SoLuongSps { get; set; }
    public DbSet<DonHang> DonHangs { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
    public DbSet<PhanHoi> PhanHois { get; set; }
    public DbSet<DanhGia> DanhGias { get; set; }
    public DbSet<KhaoSat> KhaoSats { get; set; }
    public DbSet<CauHoiKhaoSat> CauHoiKhaoSats { get; set; }
    public DbSet<LuaChonCauHoi> LuaChonCauHois { get; set; }
    public DbSet<PhieuKhaoSat> PhieuKhaoSats { get; set; }
    public DbSet<TraLoiKhaoSat> TraLoiKhaoSats { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<VaiTro>().ToTable("VAITRO");
        mb.Entity<TaiKhoan>().ToTable("TAIKHOAN");
        mb.Entity<KhachHang>().ToTable("KHACHHANG");
        mb.Entity<QuanLy>().ToTable("QUANLY");
        mb.Entity<DanhMuc>().ToTable("DANHMUC");
        mb.Entity<KhachHangSoThich>().ToTable("KHACHHANG_SOTHICH");
        mb.Entity<SanPham>().ToTable("SANPHAM");
        mb.Entity<SoLuongSp>().ToTable("SOLUONGSP");
        mb.Entity<DonHang>().ToTable("DONHANG");
        mb.Entity<ChiTietDonHang>().ToTable("CHITIETDONHANG");
        mb.Entity<PhanHoi>().ToTable("PHANHOI");
        mb.Entity<DanhGia>().ToTable("DANHGIA");
        mb.Entity<KhaoSat>().ToTable("KHAOSAT");
        mb.Entity<CauHoiKhaoSat>().ToTable("CAUHOI_KHAOSAT");
        mb.Entity<LuaChonCauHoi>().ToTable("LUACHON_CAUHOI");
        mb.Entity<PhieuKhaoSat>().ToTable("PHIEUKHAOSAT");
        mb.Entity<TraLoiKhaoSat>().ToTable("TRALOI_KHAOSAT");

        mb.Entity<VaiTro>(e =>
        {
            e.HasKey(x => x.MaVaiTro);
            e.HasIndex(x => x.TenVaiTro).IsUnique();
            e.Property(x => x.TenVaiTro).HasMaxLength(50);
        });

        mb.Entity<TaiKhoan>(e =>
        {
            e.HasKey(x => x.MaTK);
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.Property(x => x.TenDangNhap).HasMaxLength(50);
            e.Property(x => x.MatKhau).HasMaxLength(255);
            e.HasOne(x => x.VaiTro)
                .WithMany(v => v.TaiKhoans)
                .HasForeignKey(x => x.MaVaiTro);
        });

        mb.Entity<QuanLy>(e =>
        {
            e.HasKey(x => x.MaQL);
            e.HasIndex(x => x.MaTK).IsUnique();
            e.HasOne(x => x.TaiKhoan)
                .WithOne(t => t.QuanLy)
                .HasForeignKey<QuanLy>(x => x.MaTK);
        });

        mb.Entity<KhachHang>(e =>
        {
            e.HasKey(x => x.MaKH);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.MaTK).IsUnique();
            e.Property(x => x.HoTen).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.GioiTinh).HasMaxLength(10);
            e.HasOne(x => x.TaiKhoan)
                .WithOne(t => t.KhachHang)
                .HasForeignKey<KhachHang>(x => x.MaTK);
        });

        mb.Entity<DanhMuc>(e =>
        {
            e.HasKey(x => x.MaDM);
            e.HasIndex(x => x.TenDM).IsUnique();
        });

        mb.Entity<KhachHangSoThich>(e =>
        {
            e.HasKey(x => new { x.MaKH, x.MaDM });
            e.Property(x => x.NgayChon).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.KhachHang)
                .WithMany(k => k.KhachHangSoThiches)
                .HasForeignKey(x => x.MaKH)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DanhMuc)
                .WithMany(d => d.KhachHangSoThiches)
                .HasForeignKey(x => x.MaDM)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<SanPham>(e =>
        {
            e.HasKey(x => x.MaSP);
            e.Property(x => x.TenSP).HasMaxLength(200);
            e.HasOne(x => x.DanhMuc)
                .WithMany(d => d.SanPhams)
                .HasForeignKey(x => x.MaDM);
        });

        mb.Entity<SoLuongSp>(e =>
        {
            e.HasKey(x => x.MaSL);
            e.HasIndex(x => x.SKU).IsUnique();
            e.Property(x => x.SKU).HasMaxLength(50);
            e.Property(x => x.KichThuoc).HasMaxLength(20);
            e.Property(x => x.MauSac).HasMaxLength(50);
            e.Property(x => x.GiaBan).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.SanPham)
                .WithMany(s => s.SoLuongSps)
                .HasForeignKey(x => x.MaSP)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<DonHang>(e =>
        {
            e.HasKey(x => x.MaDH);
            e.Property(x => x.TongTien).HasColumnType("decimal(18,2)");
            e.Property(x => x.TrangThai).HasMaxLength(30);
            e.HasOne(x => x.KhachHang)
                .WithMany(k => k.DonHangs)
                .HasForeignKey(x => x.MaKH);
            e.HasOne(x => x.QuanLyXuLy)
                .WithMany(q => q.DonHangsXuLy)
                .HasForeignKey(x => x.MaQLXuLy)
                .IsRequired(false);
        });

        mb.Entity<ChiTietDonHang>(e =>
        {
            e.HasKey(x => new { x.MaDH, x.MaSL });
            e.Property(x => x.DonGia).HasColumnType("decimal(18,2)");
            e.Property(x => x.GiamGia).HasColumnType("decimal(18,2)");
            e.Property(x => x.ThanhTien).HasComputedColumnSql("(([SoLuong]*[DonGia])-[GiamGia])", stored: true);
            e.HasOne(x => x.DonHang)
                .WithMany(d => d.ChiTiets)
                .HasForeignKey(x => x.MaDH)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SoLuongSp)
                .WithMany(s => s.ChiTietDonHangs)
                .HasForeignKey(x => x.MaSL);
        });

        mb.Entity<PhanHoi>(e =>
        {
            e.HasKey(x => x.MaPH);
            e.Property(x => x.TrangThai).HasMaxLength(30);
            e.HasOne(x => x.KhachHang).WithMany(k => k.PhanHois).HasForeignKey(x => x.MaKH);
            e.HasOne(x => x.SanPham).WithMany(s => s.PhanHois).HasForeignKey(x => x.MaSP).IsRequired(false);
            e.HasOne(x => x.QuanLy).WithMany(q => q.PhanHois).HasForeignKey(x => x.MaQL).IsRequired(false);
        });

        mb.Entity<DanhGia>(e =>
        {
            e.HasKey(x => x.MaDG);
            e.HasIndex(x => new { x.MaKH, x.MaSP }).IsUnique();
            e.HasCheckConstraint("CK_DANHGIA_SOSAO", "[SoSao] BETWEEN 1 AND 5");
            e.HasOne(x => x.KhachHang).WithMany(k => k.DanhGias).HasForeignKey(x => x.MaKH);
            e.HasOne(x => x.SanPham).WithMany(s => s.DanhGias).HasForeignKey(x => x.MaSP);
        });

        mb.Entity<KhaoSat>(e =>
        {
            e.HasKey(x => x.MaKS);
            e.HasOne(x => x.QuanLy).WithMany(q => q.KhaoSats).HasForeignKey(x => x.MaQL);
        });

        mb.Entity<CauHoiKhaoSat>(e =>
        {
            e.HasKey(x => x.MaCauHoi);
            e.HasOne(x => x.KhaoSat).WithMany(k => k.CauHois).HasForeignKey(x => x.MaKS);
        });

        mb.Entity<LuaChonCauHoi>(e =>
        {
            e.HasKey(x => x.MaLuaChon);
            e.HasOne(x => x.CauHoi).WithMany(c => c.LuaChons).HasForeignKey(x => x.MaCauHoi);
        });

        mb.Entity<PhieuKhaoSat>(e =>
        {
            e.HasKey(x => x.MaPhieuKS);
            e.HasIndex(x => new { x.MaKS, x.MaKH }).IsUnique();
            e.HasOne(x => x.KhaoSat).WithMany(k => k.PhieuKhaoSats).HasForeignKey(x => x.MaKS).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.KhachHang).WithMany(k => k.PhieuKhaoSats).HasForeignKey(x => x.MaKH);
        });

        mb.Entity<TraLoiKhaoSat>(e =>
        {
            e.HasKey(x => x.MaTraLoi);
            e.HasOne(x => x.PhieuKhaoSat).WithMany(p => p.TraLois).HasForeignKey(x => x.MaPhieuKS).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.CauHoi).WithMany(c => c.TraLois).HasForeignKey(x => x.MaCauHoi).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.LuaChon).WithMany().HasForeignKey(x => x.MaLuaChon).IsRequired(false);
        });

        mb.Entity<VaiTro>().HasData(
            new VaiTro { MaVaiTro = 1, TenVaiTro = "Quản lý", MoTa = "Quản trị viên / Quản lý hệ thống" },
            new VaiTro { MaVaiTro = 2, TenVaiTro = "Khách hàng", MoTa = "Khách hàng mua sắm thời trang nam" }
        );

        mb.Entity<TaiKhoan>().HasData(
            new TaiKhoan { MaTK = 1, TenDangNhap = "admin", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 1, TrangThai = true, NgayTao = new DateTime(2026, 09, 20) },
            new TaiKhoan { MaTK = 2, TenDangNhap = "manager01", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 1, TrangThai = true, NgayTao = new DateTime(2026, 09, 20) },
            new TaiKhoan { MaTK = 3, TenDangNhap = "customer1", MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"), MaVaiTro = 2, TrangThai = true, NgayTao = new DateTime(2026, 09, 20) }
        );
    }
}

using Microsoft.EntityFrameworkCore;
using Fashion_store.Models;

namespace Fashion_store.Data
{
    public class FashionStoreDbContext : DbContext
    {
        public FashionStoreDbContext(DbContextOptions<FashionStoreDbContext> options)
            : base(options)
        {
        }

        public DbSet<VaiTro> VaiTro { get; set; }
        public DbSet<TaiKhoan> TaiKhoan { get; set; }
        public DbSet<QuanLy> QuanLy { get; set; }
        public DbSet<KhachHang> KhachHang { get; set; }
        public DbSet<DanhMuc> DanhMuc { get; set; }
        public DbSet<SanPham> SanPham { get; set; }
        public DbSet<SoLuongSp> SoLuongSp { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bảng VAITRO
            modelBuilder.Entity<VaiTro>(entity =>
            {
                entity.ToTable("VAITRO");
                entity.HasKey(e => e.MaVaiTro);
                entity.Property(e => e.TenVaiTro).IsRequired().HasMaxLength(50);
            });

            // Bảng TAIKHOAN
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.ToTable("TAIKHOAN");
                entity.HasKey(e => e.MaTK);
                entity.Property(e => e.TenDangNhap).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MatKhau).IsRequired().HasMaxLength(255);

                entity.HasOne(d => d.VaiTro)
                    .WithMany(p => p.TaiKhoans)
                    .HasForeignKey(d => d.MaVaiTro)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TAIKHOAN_VAITRO");
            });

            // Bảng QUANLY
            modelBuilder.Entity<QuanLy>(entity =>
            {
                entity.ToTable("QUANLY");
                entity.HasKey(e => e.MaQL);
                entity.Property(e => e.HoTen).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.TaiKhoan)
                    .WithOne(p => p.QuanLy)
                    .HasForeignKey<QuanLy>(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_QUANLY_TAIKHOAN");
            });

            // Bảng KHACHHANG
            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.ToTable("KHACHHANG");
                entity.HasKey(e => e.MaKH);
                entity.Property(e => e.HoTen).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.TaiKhoan)
                    .WithOne(p => p.KhachHang)
                    .HasForeignKey<KhachHang>(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_KHACHHANG_TAIKHOAN");
            });

            // Bảng DANHMUC
            modelBuilder.Entity<DanhMuc>(entity =>
            {
                entity.ToTable("DANHMUC");
                entity.HasKey(e => e.MaDM);
                entity.Property(e => e.TenDM).IsRequired().HasMaxLength(100);
            });

            // Bảng SANPHAM
            modelBuilder.Entity<SanPham>(entity =>
            {
                entity.ToTable("SANPHAM");
                entity.HasKey(e => e.MaSP);
                entity.Property(e => e.TenSP).IsRequired().HasMaxLength(200);

                entity.HasOne(d => d.DanhMuc)
                    .WithMany(p => p.SanPhams)
                    .HasForeignKey(d => d.MaDM)
                    .HasConstraintName("FK_SANPHAM_DANHMUC");
            });

            // Bảng SOLUONGSP (Biến thể)
            modelBuilder.Entity<SoLuongSp>(entity =>
            {
                entity.ToTable("SOLUONGSP");
                entity.HasKey(e => e.MaSL);
                entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
                entity.Property(e => e.GiaBan).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.SanPham)
                    .WithMany(p => p.SoLuongSps)
                    .HasForeignKey(d => d.MaSP)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_SOLUONGSP_SANPHAM");
            });
        }
    }
}

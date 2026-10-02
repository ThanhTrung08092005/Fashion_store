using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_store.Models
{
    // ==========================================
    // 1. BẢNG CHIẾN DỊCH KHẢO SÁT (KHAOSAT)
    // ==========================================
    [Table("KHAOSAT")]
    public class KhaoSat
    {
        [Key]
        public int MaKS { get; set; }

        public int MaQL { get; set; }

        [Required]
        [StringLength(200)]
        public string TenKhaoSat { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        public bool TrangThai { get; set; } = true;

        // Khóa ngoại liên kết
        [ForeignKey("MaQL")]
        public virtual QuanLy? QuanLy { get; set; }

        public virtual ICollection<CauHoiKhaoSat> CauHoiKhaoSats { get; set; } = new List<CauHoiKhaoSat>();
        public virtual ICollection<PhieuKhaoSat> PhieuKhaoSats { get; set; } = new List<PhieuKhaoSat>();
    }

    // ==========================================
    // 2. BẢNG CÂU HỎI KHẢO SÁT (CAUHOI_KHAOSAT)
    // ==========================================
    [Table("CAUHOI_KHAOSAT")]
    public class CauHoiKhaoSat
    {
        [Key]
        public int MaCauHoi { get; set; }

        public int MaKS { get; set; }

        [Required]
        [StringLength(1000)]
        public string NoiDung { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string LoaiCauHoi { get; set; } = "Một lựa chọn";

        public int ThuTu { get; set; } = 1;

        public bool BatBuoc { get; set; } = false;

        // Khóa ngoại liên kết
        [ForeignKey("MaKS")]
        public virtual KhaoSat? KhaoSat { get; set; }

        public virtual ICollection<LuaChonCauHoi> LuaChonCauHois { get; set; } = new List<LuaChonCauHoi>();
    }

    // ==========================================
    // 3. BẢNG CÁC ĐÁP ÁN LỰA CHỌN (LUACHON_CAUHOI)
    // ==========================================
    [Table("LUACHON_CAUHOI")]
    public class LuaChonCauHoi
    {
        [Key]
        public int MaLuaChon { get; set; }

        public int MaCauHoi { get; set; }

        [Required]
        [StringLength(500)]
        public string NoiDung { get; set; } = string.Empty;

        public int ThuTu { get; set; } = 1;

        // Khóa ngoại liên kết
        [ForeignKey("MaCauHoi")]
        public virtual CauHoiKhaoSat? CauHoiKhaoSat { get; set; }
    }

    // ==========================================
    // 4. BẢNG PHIẾU KHẢO SÁT CỦA KHÁCH HÀNG (PHIEUKHAOSAT)
    // ==========================================
    [Table("PHIEUKHAOSAT")]
    public class PhieuKhaoSat
    {
        [Key]
        public int MaPhieuKS { get; set; }

        public int MaKS { get; set; }

        public int MaKH { get; set; }

        public DateTime NgayBatDau { get; set; } = DateTime.Now;

        public DateTime? NgayNop { get; set; }

        [StringLength(30)]
        public string TrangThai { get; set; } = "Đang làm";

        // Khóa ngoại liên kết
        [ForeignKey("MaKS")]
        public virtual KhaoSat? KhaoSat { get; set; }

        [ForeignKey("MaKH")]
        public virtual KhachHang? KhachHang { get; set; }

        public virtual ICollection<TraLoiKhaoSat> TraLoiKhaoSats { get; set; } = new List<TraLoiKhaoSat>();
    }

    // ==========================================
    // 5. BẢNG CHI TIẾT CÂU TRẢ LỜI (TRALOI_KHAOSAT)
    // ==========================================
    [Table("TRALOI_KHAOSAT")]
    public class TraLoiKhaoSat
    {
        [Key]
        public int MaTraLoi { get; set; }

        public int MaPhieuKS { get; set; }

        public int MaCauHoi { get; set; }

        public int? MaLuaChon { get; set; }

        public string? NoiDungTraLoi { get; set; }

        public int? SoSao { get; set; }

        // Khóa ngoại liên kết
        [ForeignKey("MaPhieuKS")]
        public virtual PhieuKhaoSat? PhieuKhaoSat { get; set; }

        [ForeignKey("MaCauHoi")]
        public virtual CauHoiKhaoSat? CauHoiKhaoSat { get; set; }

        [ForeignKey("MaLuaChon")]
        public virtual LuaChonCauHoi? LuaChonCauHoi { get; set; }
    }

    // ==========================================
    // 6. BẢNG PHẢN HỒI Ý KIẾN KHÁCH HÀNG (PHANHOI)
    // ==========================================
    [Table("PHANHOI")]
    public class PhanHoi
    {
        [Key]
        public int MaPH { get; set; }

        public int MaKH { get; set; }

        public int? MaSP { get; set; }

        public int? MaQL { get; set; }

        [StringLength(200)]
        public string? TieuDe { get; set; }

        [Required]
        public string NoiDung { get; set; } = string.Empty;

        [StringLength(50)]
        public string? LoaiPhanHoi { get; set; }

        [Range(1, 5)]
        public int? SoSao { get; set; }

        [Required]
        [StringLength(30)]
        public string TrangThai { get; set; } = "Chưa xử lý";

        public string? PhanHoiQuanLy { get; set; }

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public DateTime? NgayXuLy { get; set; }

        // Khóa ngoại liên kết
        [ForeignKey("MaKH")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("MaSP")]
        public virtual SanPham? SanPham { get; set; }

        [ForeignKey("MaQL")]
        public virtual QuanLy? QuanLy { get; set; }
    }
}

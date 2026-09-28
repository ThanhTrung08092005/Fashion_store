-- ==============================================================================
-- CƠ SỞ DỮ LIỆU: Fashion_store (SỞ THÍCH LẤY TRỰC TIẾP TỪ BẢNG DANHMUC)
-- Chuyên Thời Trang Nam - Chuẩn hóa 3NF cho Đồ Án HTTTDN
-- ==============================================================================

CREATE DATABASE Fashion_store;
GO
USE Fashion_store;
GO

-- Xóa bảng cũ theo thứ tự khóa ngoại
IF OBJECT_ID('TRALOI_KHAOSAT', 'U') IS NOT NULL DROP TABLE TRALOI_KHAOSAT;
IF OBJECT_ID('PHIEUKHAOSAT', 'U') IS NOT NULL DROP TABLE PHIEUKHAOSAT;
IF OBJECT_ID('LUACHON_CAUHOI', 'U') IS NOT NULL DROP TABLE LUACHON_CAUHOI;
IF OBJECT_ID('CAUHOI_KHAOSAT', 'U') IS NOT NULL DROP TABLE CAUHOI_KHAOSAT;
IF OBJECT_ID('KHAOSAT', 'U') IS NOT NULL DROP TABLE KHAOSAT;
IF OBJECT_ID('DANHGIA', 'U') IS NOT NULL DROP TABLE DANHGIA;
IF OBJECT_ID('PHANHOI', 'U') IS NOT NULL DROP TABLE PHANHOI;
IF OBJECT_ID('CHITIETDONHANG', 'U') IS NOT NULL DROP TABLE CHITIETDONHANG;
IF OBJECT_ID('DONHANG', 'U') IS NOT NULL DROP TABLE DONHANG;
IF OBJECT_ID('SOLUONGSP', 'U') IS NOT NULL DROP TABLE SOLUONGSP;
IF OBJECT_ID('SANPHAM', 'U') IS NOT NULL DROP TABLE SANPHAM;
IF OBJECT_ID('KHACHHANG_SOTHICH', 'U') IS NOT NULL DROP TABLE KHACHHANG_SOTHICH;
IF OBJECT_ID('DANHMUC', 'U') IS NOT NULL DROP TABLE DANHMUC;
IF OBJECT_ID('KHACHHANG', 'U') IS NOT NULL DROP TABLE KHACHHANG;
IF OBJECT_ID('QUANLY', 'U') IS NOT NULL DROP TABLE QUANLY;
IF OBJECT_ID('TAIKHOAN', 'U') IS NOT NULL DROP TABLE TAIKHOAN;
IF OBJECT_ID('VAITRO', 'U') IS NOT NULL DROP TABLE VAITRO;
GO

-- =======================================================
-- 1. PHÂN HỆ TÀI KHOẢN & NGƯỜI DÙNG
-- =======================================================

CREATE TABLE VAITRO
(
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    MoTa NVARCHAR(255)
);
GO

CREATE TABLE TAIKHOAN
(
    MaTK INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    MaVaiTro INT NOT NULL,
    TrangThai BIT NOT NULL DEFAULT 1,             -- 1: Hoạt động, 0: Bị khóa
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_TAIKHOAN_VAITRO
        FOREIGN KEY (MaVaiTro)
        REFERENCES VAITRO(MaVaiTro)
);
GO

CREATE TABLE QUANLY
(
    MaQL INT IDENTITY(1,1) PRIMARY KEY,
    MaTK INT NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    SoDienThoai VARCHAR(15),
    ChucVu NVARCHAR(50),
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_QUANLY_TAIKHOAN
        FOREIGN KEY (MaTK)
        REFERENCES TAIKHOAN(MaTK)
);
GO

-- Bảng Hồ sơ Khách hàng
CREATE TABLE KHACHHANG
(
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    MaTK INT NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15),
    NgaySinh DATE,                                -- Tính tỷ lệ độ tuổi trong CRM
    GioiTinh NVARCHAR(10) DEFAULT N'Nam',
    DiaChi NVARCHAR(255),
    NgayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
    DaXoa BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_KHACHHANG_TAIKHOAN
        FOREIGN KEY (MaTK)
        REFERENCES TAIKHOAN(MaTK)
);
GO

-- =======================================================
-- 2. PHÂN HỆ DANH MỤC SẢN PHẨM & SỞ THÍCH THỜI TRANG NAM
-- =======================================================

-- Bảng DANHMUC: Vừa là danh mục sản phẩm của shop, vừa là danh mục sở thích
CREATE TABLE DANHMUC
(
    MaDM INT IDENTITY(1,1) PRIMARY KEY,
    TenDM NVARCHAR(100) NOT NULL UNIQUE,         -- Áo thun nam, Áo Polo nam, Sơ mi cổ tàu...
    MoTa NVARCHAR(500),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

-- Bảng KHACHHANG_SOTHICH: Nối MaKH với MaDM (Quan hệ Nhiều - Nhiều)
CREATE TABLE KHACHHANG_SOTHICH
(
    MaKH INT NOT NULL,
    MaDM INT NOT NULL,
    NgayChon DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_KHACHHANG_SOTHICH 
        PRIMARY KEY (MaKH, MaDM),

    CONSTRAINT FK_KHST_KHACHHANG 
        FOREIGN KEY (MaKH) 
        REFERENCES KHACHHANG(MaKH) 
        ON DELETE CASCADE,

    CONSTRAINT FK_KHST_DANHMUC 
        FOREIGN KEY (MaDM) 
        REFERENCES DANHMUC(MaDM) 
        ON DELETE CASCADE
);
GO

-- =======================================================
-- 3. PHÂN HỆ SẢN PHẨM & BIẾN THỂ (SIZE / MÀU)
-- =======================================================

CREATE TABLE SANPHAM
(
    MaSP INT IDENTITY(1,1) PRIMARY KEY,
    MaDM INT NOT NULL,
    TenSP NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    ChatLieu NVARCHAR(100),                       -- Cotton, Bamboo, Spandex, Denim, Kaki...
    TrangThai BIT NOT NULL DEFAULT 1,             -- 1: Đang bán, 0: Ẩn
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_SANPHAM_DANHMUC
        FOREIGN KEY (MaDM)
        REFERENCES DANHMUC(MaDM)
);
GO

CREATE TABLE SOLUONGSP
(
    MaSL INT IDENTITY(1,1) PRIMARY KEY,
    MaSP INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    KichThuoc NVARCHAR(20) NOT NULL,              -- S, M, L, XL, XXL, 29, 30, 31, 32...
    MauSac NVARCHAR(50) NOT NULL,                 -- Đen, Trắng, Xám, Xanh Navy, Be...
    GiaBan DECIMAL(18,2) NOT NULL,
    SoLuongTon INT NOT NULL DEFAULT 0,
    HinhAnh NVARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_SOLUONGSP_SANPHAM
        FOREIGN KEY (MaSP)
        REFERENCES SANPHAM(MaSP)
        ON DELETE CASCADE,

    CONSTRAINT CK_SOLUONGSP_GIA CHECK (GiaBan >= 0),
    CONSTRAINT CK_SOLUONGSP_SOLUONG CHECK (SoLuongTon >= 0)
);
GO

-- =======================================================
-- 4. PHÂN HỆ ĐƠN HÀNG & CHI TIẾT ĐƠN HÀNG
-- =======================================================

CREATE TABLE DONHANG
(
    MaDH INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaQLXuLy INT NULL,
    NgayDat DATETIME NOT NULL DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận',
    DiaChiGiaoHang NVARCHAR(255) NOT NULL,
    SoDienThoaiNhan VARCHAR(15),
    GhiChu NVARCHAR(500) NULL,

    CONSTRAINT FK_DONHANG_KHACHHANG
        FOREIGN KEY (MaKH)
        REFERENCES KHACHHANG(MaKH),

    CONSTRAINT FK_DONHANG_QUANLY
        FOREIGN KEY (MaQLXuLy)
        REFERENCES QUANLY(MaQL),

    CONSTRAINT CK_DONHANG_TONGTIEN CHECK (TongTien >= 0),
    CONSTRAINT CK_DONHANG_TRANGTHAI CHECK
    (
        TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đang giao', N'Đã giao', N'Đã hủy')
    )
);
GO

CREATE TABLE CHITIETDONHANG
(
    MaDH INT NOT NULL,
    MaSL INT NOT NULL,
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL,
    GiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    ThanhTien AS ((SoLuong * DonGia) - GiamGia) PERSISTED,

    CONSTRAINT PK_CHITIETDONHANG PRIMARY KEY (MaDH, MaSL),

    CONSTRAINT FK_CTDH_DONHANG
        FOREIGN KEY (MaDH)
        REFERENCES DONHANG(MaDH)
        ON DELETE CASCADE,

    CONSTRAINT FK_CTDH_SOLUONGSP
        FOREIGN KEY (MaSL)
        REFERENCES SOLUONGSP(MaSL),

    CONSTRAINT CK_CTDH_SOLUONG CHECK (SoLuong > 0),
    CONSTRAINT CK_CTDH_DONGIA CHECK (DonGia >= 0),
    CONSTRAINT CK_CTDH_GIAMGIA CHECK (GiamGia >= 0)
);
GO

-- =======================================================
-- 5. PHÂN HỆ PHẢN HỒI & ĐÁNH GIÁ (CRM)
-- =======================================================

CREATE TABLE PHANHOI
(
    MaPH INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaSP INT NULL,
    MaQL INT NULL,
    TieuDe NVARCHAR(200),
    NoiDung NVARCHAR(MAX) NOT NULL,
    LoaiPhanHoi NVARCHAR(50),
    SoSao INT NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chưa xử lý',
    PhanHoiQuanLy NVARCHAR(MAX),
    NgayGui DATETIME NOT NULL DEFAULT GETDATE(),
    NgayXuLy DATETIME NULL,

    CONSTRAINT FK_PHANHOI_KHACHHANG FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH),
    CONSTRAINT FK_PHANHOI_SANPHAM FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP),
    CONSTRAINT FK_PHANHOI_QUANLY FOREIGN KEY (MaQL) REFERENCES QUANLY(MaQL),
    CONSTRAINT CK_PHANHOI_SOSAO CHECK (SoSao IS NULL OR SoSao BETWEEN 1 AND 5),
    CONSTRAINT CK_PHANHOI_TRANGTHAI CHECK (TrangThai IN (N'Chưa xử lý', N'Đang xử lý', N'Đã xử lý', N'Đã đóng'))
);
GO

CREATE TABLE DANHGIA
(
    MaDG INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaSP INT NOT NULL,
    SoSao INT NOT NULL,
    NoiDung NVARCHAR(1000),
    NgayDanhGia DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_DANHGIA_KHACHHANG FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH),
    CONSTRAINT FK_DANHGIA_SANPHAM FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP),
    CONSTRAINT CK_DANHGIA_SOSAO CHECK (SoSao BETWEEN 1 AND 5),
    CONSTRAINT UQ_DANHGIA_KH_SP UNIQUE (MaKH, MaSP)
);
GO

-- =======================================================
-- 6. PHÂN HỆ KHẢO SÁT MẪU ÁO MỚI (CRM)
-- =======================================================

CREATE TABLE KHAOSAT
(
    MaKS INT IDENTITY(1,1) PRIMARY KEY,
    MaQL INT NOT NULL,
    TenKhaoSat NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NOT NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_KHAOSAT_QUANLY FOREIGN KEY (MaQL) REFERENCES QUANLY(MaQL),
    CONSTRAINT CK_KHAOSAT_NGAY CHECK (NgayKetThuc >= NgayBatDau)
);
GO

CREATE TABLE CAUHOI_KHAOSAT
(
    MaCauHoi INT IDENTITY(1,1) PRIMARY KEY,
    MaKS INT NOT NULL,
    NoiDung NVARCHAR(1000) NOT NULL,
    LoaiCauHoi NVARCHAR(30) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 1,
    BatBuoc BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_CAUHOI_KHAOSAT FOREIGN KEY (MaKS) REFERENCES KHAOSAT(MaKS),
    CONSTRAINT CK_CAUHOI_LOAI CHECK (LoaiCauHoi IN (N'Một lựa chọn', N'Nhiều lựa chọn', N'Tự luận', N'Đánh giá sao'))
);
GO

CREATE TABLE LUACHON_CAUHOI
(
    MaLuaChon INT IDENTITY(1,1) PRIMARY KEY,
    MaCauHoi INT NOT NULL,
    NoiDung NVARCHAR(500) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_LUACHON_CAUHOI FOREIGN KEY (MaCauHoi) REFERENCES CAUHOI_KHAOSAT(MaCauHoi)
);
GO

CREATE TABLE PHIEUKHAOSAT
(
    MaPhieuKS INT IDENTITY(1,1) PRIMARY KEY,
    MaKS INT NOT NULL,
    MaKH INT NOT NULL,
    NgayBatDau DATETIME NOT NULL DEFAULT GETDATE(),
    NgayNop DATETIME NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Đang làm',

    CONSTRAINT FK_PHIEUKS_KHAOSAT FOREIGN KEY (MaKS) REFERENCES KHAOSAT(MaKS),
    CONSTRAINT FK_PHIEUKS_KHACHHANG FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH),
    CONSTRAINT CK_PHIEUKS_TRANGTHAI CHECK (TrangThai IN (N'Đang làm', N'Đã hoàn thành')),
    CONSTRAINT UQ_PHIEUKS_KH_KS UNIQUE (MaKS, MaKH)
);
GO

CREATE TABLE TRALOI_KHAOSAT
(
    MaTraLoi INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuKS INT NOT NULL,
    MaCauHoi INT NOT NULL,
    MaLuaChon INT NULL,
    NoiDungTraLoi NVARCHAR(MAX),
    SoSao INT NULL,

    CONSTRAINT FK_TRALOI_PHIEUKS FOREIGN KEY (MaPhieuKS) REFERENCES PHIEUKHAOSAT(MaPhieuKS),
    CONSTRAINT FK_TRALOI_CAUHOI FOREIGN KEY (MaCauHoi) REFERENCES CAUHOI_KHAOSAT(MaCauHoi),
    CONSTRAINT FK_TRALOI_LUACHON FOREIGN KEY (MaLuaChon) REFERENCES LUACHON_CAUHOI(MaLuaChon),
    CONSTRAINT CK_TRALOI_SOSAO CHECK (SoSao BETWEEN 1 AND 5)
);
GO

-- ==============================================================================
-- DỮ LIỆU KHỞI TẠO MẪU (CHUẨN THỜI TRANG NAM 100%)
-- ==============================================================================

-- 1. Vai trò
INSERT INTO VAITRO (TenVaiTro, MoTa) VALUES 
(N'Quản lý', N'Quản trị viên / Quản lý hệ thống'),
(N'Khách hàng', N'Khách hàng mua sắm thời trang nam');

-- 2. 10 Danh mục sản phẩm thời trang nam (đồng thời là danh mục sở thích)
INSERT INTO DANHMUC (TenDM, MoTa) VALUES 
(N'Áo Thun Nam Oversize', N'Áo thun cotton thoáng mát, form rộng Streetwear'),
(N'Áo Polo Nam Dệt Tổ Ong', N'Áo polo nam lịch sự, dệt bo cổ thoáng khí'),
(N'Áo Sơ Mi Nam Công Sở', N'Áo sơ mi dài tay chống nhăn, phom đứng lịch thiệp'),
(N'Áo Sơ Mi Cổ Tàu', N'Áo sơ mi cổ tàu hiện đại, phong cách tối giản'),
(N'Áo Khoác Gió Nam', N'Áo khoác 2 lớp cản gió, kháng nước nhẹ'),
(N'Áo Hoodie & Sweater Nam', N'Áo nỉ ấm áp, thời trang Thu Đông trẻ trung'),
(N'Quần Jean Nam Ống Suông', N'Quần jean denim wash phong cách retro cá tính'),
(N'Quần Tây & Kaki Nam', N'Quần âu công sở cao cấp, đứng phom tôn dáng'),
(N'Quần Short Nam Dạo Phố', N'Quần short đùi kaki, short nỉ thể thao năng động'),
(N'Đồ Bộ Thể Thao Nam', N'Bộ quần áo thể thao co giãn 4 chiều chuyên vận động');

-- 3. Tài khoản Quản trị viên & Nhân sự CRM
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('admin', '123456', 1, 1, GETDATE()),
('manager01', '123456', 1, 1, GETDATE()),
('sales01', '123456', 1, 1, GETDATE()),
('kho01', '123456', 1, 1, GETDATE()),
('crm01', '123456', 1, 1, GETDATE());

INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(1, N'Quản Trị Viên Hệ Thống', 'admin@fashionstore.com', '0901234567', N'Admin', 1),
(2, N'Trần Đức Long', 'manager@fashionstore.com', '0901234001', N'Quản lý Cửa Hàng', 1),
(3, N'Lê Thị Mai', 'sales@fashionstore.com', '0901234002', N'Nhân viên Bán Hàng', 1),
(4, N'Nguyễn Văn Hùng', 'kho@fashionstore.com', '0901234003', N'Nhân viên Kho', 1),
(5, N'Phạm Thu Hà', 'cskh@fashionstore.com', '0901234004', N'Nhân viên Chăm Sóc Khách Hàng', 1);

-- 4. Khởi tạo 19 Khách hàng Nam mẫu (Đầy đủ độ tuổi: <18, 18-24, 25-35, >35)
DECLARE @KhachHangTable TABLE (
    TenDN VARCHAR(50), HoTen NVARCHAR(100), Email VARCHAR(100), SDT VARCHAR(15), NgaySinh DATE, DiaChi NVARCHAR(255), TrangThai BIT
);

INSERT INTO @KhachHangTable VALUES
('khachhang01', N'Nguyễn Văn An', 'an.nguyen@gmail.com', '0912345678', '1998-05-15', N'Số 45 Cầu Giấy, Hà Nội', 1),
('khachhang02', N'Trần Quốc Bảo', 'bao.tran@gmail.com', '0987654321', '2001-10-20', N'Số 12 Lê Lợi, TP.HCM', 1),
('khachhang03', N'Nguyễn Hoàng Nam', 'nam.hoang@gmail.com', '0905123456', '2004-03-12', N'Số 120 Hai Bà Trưng, Quận 1, TP.HCM', 1),
('khachhang04', N'Lê Minh Tuấn', 'tuan.le@gmail.com', '0918765432', '2005-08-25', N'Số 45 Cầu Giấy, Hà Nội', 1),
('khachhang05', N'Trần Đình Khôi', 'khoi.tran@gmail.com', '0982345678', '1996-11-14', N'Số 88 Nguyễn Văn Linh, Đà Nẵng', 1),
('khachhang06', N'Phạm Minh Triết', 'triet.pham@gmail.com', '0971234567', '2003-01-09', N'Số 15 đường 30/4, Ninh Kiều, Cần Thơ', 1),
('khachhang07', N'Đỗ Hữu Thắng', 'thang.dohuu@gmail.com', '0938765432', '2000-05-18', N'Số 22 Lạch Tray, Ngô Quyền, Hải Phòng', 1),
('khachhang08', N'Vũ Đức Khang', 'khang.vuduc@gmail.com', '0961234999', '2009-07-22', N'Số 304 Hoàng Diệu, Quận 4, TP.HCM', 1),
('khachhang09', N'Hoàng Quốc Trung', 'trung.hoang@gmail.com', '0945678123', '1988-12-03', N'Số 72 Trần Duy Hưng, Cầu Giấy, Hà Nội', 1),
('khachhang10', N'Đặng Tuấn Kiệt', 'kiet.dang@gmail.com', '0923456789', '2004-09-30', N'Số 14 Đại lộ Bình Dương, Thủ Dầu Một', 1),
('khachhang11', N'Bùi Quang Dũng', 'dung.bui@gmail.com', '0919876543', '1998-04-16', N'Số 59 Cách Mạng Tháng 8, Quận 3, TP.HCM', 1),
('khachhang12', N'Ngô Quang Huy', 'huy.ngoquang@gmail.com', '0908765432', '1994-06-20', N'Số 102 Phạm Văn Thuận, Biên Hòa, Đồng Nai', 1),
('khachhang13', N'Trương Gia Thịnh', 'thinh.truong@gmail.com', '0978901234', '2010-02-11', N'Số 86 Thùy Vân, TP. Vũng Tàu', 1),
('khachhang14', N'Phan Văn Tài', 'tai.phanvan@gmail.com', '0934567890', '2002-10-08', N'Số 19 Lê Văn Sỹ, Phú Nhuận, TP.HCM', 1),
('khachhang15', N'Nguyễn Hoàng Bách', 'bach.nguyen@gmail.com', '0989012345', '1985-03-27', N'Số 210 Phan Bội Châu, Tam Kỳ, Quảng Nam', 1),
('khachhang16', N'Lâm Chí Vĩ', 'vi.lamchi@gmail.com', '0912340987', '2006-12-05', N'Số 63 Trần Hưng Đạo, Long Xuyên, An Giang', 1),
('khachhang17', N'Đinh Ngọc Khánh', 'khanh.dinh@gmail.com', '0965432109', '1997-08-19', N'Số 75 Hùng Vương, Nha Trang, Khánh Hòa', 1),
('khachhang18', N'Tạ Hữu Phước', 'phuoc.tahuu@gmail.com', '0901239876', '2005-04-02', N'Số 42 Tây Sơn, Đống Đa, Hà Nội', 1),
('khachhang19', N'Đoàn Văn Vũ', 'vu.doan@gmail.com', '0932109876', '2001-09-14', N'Số 18 Võ Văn Kiệt, Quận 5, TP.HCM', 0); -- Tài khoản khóa

DECLARE @tTen VARCHAR(50), @tTenHT NVARCHAR(100), @tEmail VARCHAR(100), @tSDT VARCHAR(15), @tNS DATE, @tDC NVARCHAR(255), @tTT BIT;
DECLARE @curMaTK INT;

DECLARE c CURSOR FOR SELECT TenDN, HoTen, Email, SDT, NgaySinh, DiaChi, TrangThai FROM @KhachHangTable;
OPEN c;
FETCH NEXT FROM c INTO @tTen, @tTenHT, @tEmail, @tSDT, @tNS, @tDC, @tTT;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao)
    VALUES (@tTen, '123456', 2, @tTT, GETDATE());
    SET @curMaTK = SCOPE_IDENTITY();

    INSERT INTO KHACHHANG (MaTK, HoTen, Email, SoDienThoai, NgaySinh, GioiTinh, DiaChi, NgayDangKy, DaXoa)
    VALUES (@curMaTK, @tTenHT, @tEmail, @tSDT, @tNS, N'Nam', @tDC, GETDATE(), 0);

    FETCH NEXT FROM c INTO @tTen, @tTenHT, @tEmail, @tSDT, @tNS, @tDC, @tTT;
END;
CLOSE c;
DEALLOCATE c;

-- 5. Gán trực tiếp Danh mục ưa thích (MaDM) cho từng khách hàng vào bảng KHACHHANG_SOTHICH
-- MaDM tương ứng:
-- 1: Áo Thun Nam Oversize     | 2: Áo Polo Nam Dệt Tổ Ong | 3: Áo Sơ Mi Nam Công Sở
-- 4: Áo Sơ Mi Cổ Tàu          | 5: Áo Khoác Gió Nam       | 6: Áo Hoodie & Sweater Nam
-- 7: Quần Jean Nam Ống Suông  | 8: Quần Tây & Kaki Nam    | 9: Quần Short Nam Dạo Phố | 10: Đồ Bộ Thể Thao Nam

INSERT INTO KHACHHANG_SOTHICH (MaKH, MaDM) VALUES
(1, 1), (1, 2), (1, 7),        -- KH 1 thích: Áo thun, Polo, Quần Jean
(2, 2), (2, 3), (2, 8),        -- KH 2 thích: Polo, Sơ mi công sở, Quần âu
(3, 1), (3, 6), (3, 7),        -- KH 3 thích: Áo thun, Hoodie, Quần Jean
(4, 1), (4, 9), (4, 10),       -- KH 4 thích: Áo thun, Quần short, Đồ thể thao
(5, 3), (5, 4), (5, 8),        -- KH 5 thích: Sơ mi công sở, Sơ mi cổ tàu, Quần âu
(6, 1), (6, 5), (6, 7),        -- KH 6 thích: Áo thun, Áo khoác gió, Quần Jean
(7, 2), (7, 4), (7, 8),        -- KH 7 thích: Polo, Sơ mi cổ tàu, Quần âu
(8, 1), (8, 6), (8, 9),        -- KH 8 (<18 tuổi) thích: Áo thun, Hoodie, Quần short
(9, 3), (9, 5), (9, 8),        -- KH 9 (>35 tuổi) thích: Sơ mi công sở, Áo khoác gió, Quần âu
(10, 2), (10, 9), (10, 10),    -- KH 10 thích: Polo, Quần short, Đồ thể thao
(11, 3), (11, 4), (11, 8),     -- KH 11 thích: Sơ mi công sở, Sơ mi cổ tàu, Quần âu
(12, 2), (12, 5), (12, 7),     -- KH 12 thích: Polo, Áo khoác gió, Quần Jean
(13, 1), (13, 6), (13, 9),     -- KH 13 (<18 tuổi) thích: Áo thun, Hoodie, Quần short
(14, 1), (14, 5), (14, 7),     -- KH 14 thích: Áo thun, Áo khoác gió, Quần Jean
(15, 3), (15, 5), (15, 8),     -- KH 15 (>35 tuổi) thích: Sơ mi công sở, Áo khoác gió, Quần âu
(16, 2), (16, 4), (16, 9),     -- KH 16 thích: Polo, Sơ mi cổ tàu, Quần short
(17, 3), (17, 4), (17, 8),     -- KH 17 thích: Sơ mi công sở, Sơ mi cổ tàu, Quần âu
(18, 1), (18, 9), (18, 10),    -- KH 18 thích: Áo thun, Quần short, Đồ thể thao
(19, 1), (19, 2), (19, 7);     -- KH 19 thích: Áo thun, Polo, Quần Jean

-- 6. Sản phẩm nam mẫu gắn với MaDM
INSERT INTO SANPHAM (MaDM, TenSP, ChatLieu, MoTa, TrangThai) VALUES
(1, N'Áo Thun Nam Cotton Oversize Graphic Atino', N'Cotton 100% 250gsm', N'Áo phông nam phom rộng trẻ trung, thoáng mát', 1),
(2, N'Áo Polo Nam Dệt Tổ Ong Phối Bo Cổ', N'CVC dệt tổ ong', N'Áo polo nam lịch sự, co giãn 4 chiều mềm mịn', 1),
(3, N'Áo Sơ Mi Nam Tay Dài Cổ Đức Trắng', N'Vải Modal chống nhăn', N'Áo sơ mi nam công sở lịch thiệp, dễ là ủi', 1);

INSERT INTO SOLUONGSP (MaSP, SKU, KichThuoc, MauSac, GiaBan, SoLuongTon, TrangThai) VALUES
(1, 'AT01-M-BLK', 'M', N'Đen', 250000, 50, 1),
(1, 'AT01-L-BLK', 'L', N'Đen', 250000, 45, 1),
(2, 'PL02-L-WHT', 'L', N'Trắng', 350000, 30, 1),
(3, 'SM03-XL-BLU', 'XL', N'Xanh nhạt', 420000, 20, 1);

-- 7. Khởi tạo 2 bài khảo sát mẫu áo mới (KHAOSAT)
DECLARE @MaKS1 INT, @MaKS2 INT;
DECLARE @MaCH1 INT, @MaCH2 INT, @MaCH3 INT;

-- Khảo sát 1: Áo Polo nam
INSERT INTO KHAOSAT (MaQL, TenKhaoSat, MoTa, NgayBatDau, NgayKetThuc, TrangThai)
VALUES (5, N'Khảo sát Mẫu Áo Polo Nam Dệt Tổ Ong Thu Đông 2026', N'Khảo sát chất liệu và mức giá dự kiến cho dòng áo Polo nam phối bo cổ mới.', '2026-09-20', '2026-10-31', 1);
SET @MaKS1 = SCOPE_IDENTITY();

INSERT INTO CAUHOI_KHAOSAT (MaKS, NoiDung, LoaiCauHoi, ThuTu, BatBuoc) VALUES 
(@MaKS1, N'Bạn ưu tiên chất liệu nào nhất khi chọn mua áo Polo nam?', N'Một lựa chọn', 1, 1);
SET @MaCH1 = SCOPE_IDENTITY();
INSERT INTO LUACHON_CAUHOI (MaCauHoi, NoiDung, ThuTu) VALUES
(@MaCH1, N'Cotton 100% dệt tổ ong thoáng khí', 1),
(@MaCH1, N'Vải CVC pha Spandex co giãn 4 chiều', 2),
(@MaCH1, N'Sợi Bamboo (Tre) mềm mát kháng khuẩn', 3);

INSERT INTO CAUHOI_KHAOSAT (MaKS, NoiDung, LoaiCauHoi, ThuTu, BatBuoc) VALUES 
(@MaKS1, N'Mức giá bạn sẵn sàng chi trả cho mẫu áo Polo này?', N'Một lựa chọn', 2, 1);
SET @MaCH2 = SCOPE_IDENTITY();
INSERT INTO LUACHON_CAUHOI (MaCauHoi, NoiDung, ThuTu) VALUES
(@MaCH2, N'Dưới 300.000 VNĐ', 1),
(@MaCH2, N'Từ 300.000 đến 450.000 VNĐ', 2),
(@MaCH2, N'Trên 450.000 VNĐ', 3);

-- Khảo sát 2: Áo sơ mi nam cổ tàu
INSERT INTO KHAOSAT (MaQL, TenKhaoSat, MoTa, NgayBatDau, NgayKetThuc, TrangThai)
VALUES (5, N'Khảo sát Nhu cầu Dòng Áo Sơ Mi Nam Cổ Tàu Kháng Nhăn', N'Thăm dò ý kiến về phom dáng sơ mi nam hiện đại chống nhăn.', '2026-09-22', '2026-11-15', 1);
SET @MaKS2 = SCOPE_IDENTITY();

INSERT INTO CAUHOI_KHAOSAT (MaKS, NoiDung, LoaiCauHoi, ThuTu, BatBuoc) VALUES 
(@MaKS2, N'Tính năng nào bạn đánh giá cao nhất ở sơ mi nam Atino?', N'Một lựa chọn', 1, 1);
SET @MaCH3 = SCOPE_IDENTITY();
INSERT INTO LUACHON_CAUHOI (MaCauHoi, NoiDung, ThuTu) VALUES
(@MaCH3, N'Khả năng chống nhăn / hạn chế nhàu vải', 1),
(@MaCH3, N'Chất vải thoáng mát thấm hút mồ hôi', 2),
(@MaCH3, N'Cổ áo đứng phom lịch thiệp', 3);

-- Phát phiếu khảo sát cho khách hàng và lưu câu trả lời mẫu
INSERT INTO PHIEUKHAOSAT (MaKS, MaKH, NgayBatDau, NgayNop, TrangThai) VALUES
(@MaKS1, 1, '2026-09-22', '2026-09-22', N'Đã hoàn thành'),
(@MaKS1, 2, '2026-09-23', '2026-09-23', N'Đã hoàn thành'),
(@MaKS1, 3, '2026-09-24', NULL, N'Đang làm');

-- 8. Phản hồi mẫu từ khách hàng (PHANHOI)
INSERT INTO PHANHOI (MaKH, MaSP, MaQL, TieuDe, NoiDung, LoaiPhanHoi, TrangThai, PhanHoiQuanLy, NgayGui) VALUES
(1, 1, 5, N'Đánh giá áo thun', N'Form áo chuẩn oversize, chất cotton 100% 250gsm mặc mát và đứng phom. Sẽ ủng hộ shop dài dài!', N'Chất lượng', N'Đã xử lý', N'Cảm ơn bạn An đã ủng hộ shop ạ!', '2026-09-22'),
(2, 3, NULL, N'Góp ý áo sơ mi', N'Vải áo mềm mịn, hạn chế nhăn rất tốt. Nhưng phần cổ tay áo hơi rộng một chút đối với người tay gầy.', N'Form dáng', N'Chưa xử lý', NULL, '2026-09-24'),
(3, 2, NULL, N'Hỏi về áo Polo', N'Áo mặc đẹp nhưng sau 2 lần giặt màu vải hơi bị xỉn nhẹ. Shop kiểm tra lại độ bền màu nhé.', N'Chất lượng', N'Chưa xử lý', NULL, '2026-09-25');

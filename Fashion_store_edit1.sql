-- ==============================================================================
-- CƠ SỞ DỮ LIỆU: Fashion_store 
-- Chuyên Thời Trang Nam - Chuẩn hóa 3NF cho Đồ Án HTTTDN (Tích hợp CRM)
-- ==============================================================================

-- 1. Xử lý tạo Database (Chỉ tạo nếu chưa tồn tại)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Fashion_store')
BEGIN
    CREATE DATABASE Fashion_store;
END
GO

USE Fashion_store;
GO

-- ==============================================================================
-- 2. XÓA BẢNG CŨ (Xóa theo thứ tự từ Bảng con -> Bảng cha)
-- ==============================================================================
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

-- ==============================================================================
-- 3. TẠO CÁC BẢNG (Đã gộp sẵn tất cả các cột mở rộng)
-- ==============================================================================

-- Bảng Vai Trò
CREATE TABLE VAITRO
(
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    MoTa NVARCHAR(255)
);
GO

-- Bảng Tài Khoản
CREATE TABLE TAIKHOAN
(
    MaTK INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    MaVaiTro INT NOT NULL,
    TrangThai BIT NOT NULL DEFAULT 1,             
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_TAIKHOAN_VAITRO FOREIGN KEY (MaVaiTro) REFERENCES VAITRO(MaVaiTro)
);
GO

-- Bảng Quản Lý (Nhân viên / Admin)
CREATE TABLE QUANLY
(
    MaQL INT IDENTITY(1,1) PRIMARY KEY,
    MaTK INT NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    SoDienThoai VARCHAR(15),
    NgaySinh DATE NULL,
    GioiTinh NVARCHAR(10) NULL,
    ChucVu NVARCHAR(50),
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_QUANLY_TAIKHOAN FOREIGN KEY (MaTK) REFERENCES TAIKHOAN(MaTK)
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
    NgaySinh DATE,                                
    GioiTinh NVARCHAR(10) DEFAULT N'Nam',
    DiaChi NVARCHAR(255),
    NgayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
    DaXoa BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_KHACHHANG_TAIKHOAN FOREIGN KEY (MaTK) REFERENCES TAIKHOAN(MaTK)
);
GO

-- Bảng Danh Mục Sản Phẩm
CREATE TABLE DANHMUC
(
    MaDM INT IDENTITY(1,1) PRIMARY KEY,
    TenDM NVARCHAR(100) NOT NULL UNIQUE,         
    MoTa NVARCHAR(500),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

-- Bảng Sở Thích Khách Hàng
CREATE TABLE KHACHHANG_SOTHICH
(
    MaKH INT NOT NULL,
    MaDM INT NOT NULL,
    NgayChon DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_KHACHHANG_SOTHICH PRIMARY KEY (MaKH, MaDM),
    CONSTRAINT FK_KHST_KHACHHANG FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH) ON DELETE CASCADE,
    CONSTRAINT FK_KHST_DANHMUC FOREIGN KEY (MaDM) REFERENCES DANHMUC(MaDM) ON DELETE CASCADE
);
GO

-- Bảng Sản Phẩm
CREATE TABLE SANPHAM
(
    MaSP INT IDENTITY(1,1) PRIMARY KEY,
    MaDM INT NOT NULL,
    TenSP NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    ChatLieu NVARCHAR(100),
    HinhAnh VARCHAR(500) NULL,
    HinhAnhMoTa VARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    DaXoa BIT NOT NULL DEFAULT 0,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_SANPHAM_DANHMUC FOREIGN KEY (MaDM) REFERENCES DANHMUC(MaDM)
);
GO

-- Bảng Biến Thể Số Lượng Sản Phẩm (Màu / Size)
CREATE TABLE SOLUONGSP
(
    MaSL INT IDENTITY(1,1) PRIMARY KEY,
    MaSP INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    KichThuoc NVARCHAR(20) NOT NULL,              
    MauSac NVARCHAR(50) NOT NULL,                 
    GiaBan DECIMAL(18,2) NOT NULL,
    SoLuongTon INT NOT NULL DEFAULT 0,
    HinhAnh NVARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_SOLUONGSP_SANPHAM FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP) ON DELETE CASCADE,
    CONSTRAINT CK_SOLUONGSP_GIA CHECK (GiaBan >= 0),
    CONSTRAINT CK_SOLUONGSP_SOLUONG CHECK (SoLuongTon >= 0)
);
GO

-- Bảng Phản Hồi (Khiếu nại / Góp ý)
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
    CONSTRAINT FK_PHANHOI_SANPHAM FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP) ON DELETE SET NULL,
    CONSTRAINT FK_PHANHOI_QUANLY FOREIGN KEY (MaQL) REFERENCES QUANLY(MaQL),
    CONSTRAINT CK_PHANHOI_SOSAO CHECK (SoSao IS NULL OR SoSao BETWEEN 1 AND 5),
    CONSTRAINT CK_PHANHOI_TRANGTHAI CHECK (TrangThai IN (N'Chưa xử lý', N'Đang xử lý', N'Đã xử lý', N'Đã đóng'))
);
GO

-- Bảng Đánh Giá Sản Phẩm
CREATE TABLE DANHGIA
(
    MaDG INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaSP INT NOT NULL,
    SoSao INT NOT NULL,
    NoiDung NVARCHAR(1000),
    NgayDanhGia DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_DANHGIA_KHACHHANG FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH),
    CONSTRAINT FK_DANHGIA_SANPHAM FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP) ON DELETE CASCADE,
    CONSTRAINT CK_DANHGIA_SOSAO CHECK (SoSao BETWEEN 1 AND 5),
    CONSTRAINT UQ_DANHGIA_KH_SP UNIQUE (MaKH, MaSP)
);
GO

-- Bảng Chiến Dịch Khảo Sát
CREATE TABLE KHAOSAT
(
    MaKS INT IDENTITY(1,1) PRIMARY KEY,
    MaQL INT NOT NULL,
    TenKhaoSat NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    NgayBatDau DATETIME NOT NULL,
    NgayKetThuc DATETIME NOT NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_KHAOSAT_QUANLY FOREIGN KEY (MaQL) REFERENCES QUANLY(MaQL),
    CONSTRAINT CK_KHAOSAT_NGAY CHECK (NgayKetThuc >= NgayBatDau)
);
GO

-- Bảng Câu Hỏi Khảo Sát
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

-- Bảng Lựa Chọn (Đáp án trắc nghiệm)
CREATE TABLE LUACHON_CAUHOI
(
    MaLuaChon INT IDENTITY(1,1) PRIMARY KEY,
    MaCauHoi INT NOT NULL,
    NoiDung NVARCHAR(500) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_LUACHON_CAUHOI FOREIGN KEY (MaCauHoi) REFERENCES CAUHOI_KHAOSAT(MaCauHoi)
);
GO

-- Bảng Phiếu Khảo Sát (Của khách hàng)
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

-- Bảng Chi Tiết Trả Lời Khảo Sát
CREATE TABLE TRALOI_KHAOSAT
(
    MaTraLoi INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuKS INT NOT NULL,
    MaCauHoi INT NOT NULL,
    MaLuaChon INT NULL,
    NoiDungTraLoi NVARCHAR(MAX),
    SoSao INT NULL,
    NgayNop DATETIME NULL,

    CONSTRAINT FK_TRALOI_PHIEUKS FOREIGN KEY (MaPhieuKS) REFERENCES PHIEUKHAOSAT(MaPhieuKS),
    CONSTRAINT FK_TRALOI_CAUHOI FOREIGN KEY (MaCauHoi) REFERENCES CAUHOI_KHAOSAT(MaCauHoi),
    CONSTRAINT FK_TRALOI_LUACHON FOREIGN KEY (MaLuaChon) REFERENCES LUACHON_CAUHOI(MaLuaChon),
    CONSTRAINT CK_TRALOI_SOSAO CHECK (SoSao BETWEEN 1 AND 5)
);
GO


-- ==============================================================================
-- 4. INSERT DỮ LIỆU MẪU (CHUẨN THỜI TRANG NAM 100%)
-- ==============================================================================

-- 4.1. Vai trò
INSERT INTO VAITRO (TenVaiTro, MoTa) VALUES 
(N'Quản lý', N'Nhân viên bán hàng và quản lý CRM'),
(N'Khách hàng', N'Khách hàng mua sắm thời trang nam'),
(N'Admin', N'Quản trị viên');

-- 4.2. Danh mục sản phẩm thời trang nam (đồng thời là danh mục sở thích)
INSERT INTO DANHMUC (TenDM, MoTa) VALUES 
(N'Áo Thun Nam Oversize', N'Áo thun cotton thoáng mát, form rộng Streetwear'),
(N'Áo Polo Nam Dệt Tổ Ong', N'Áo polo nam lịch sự, dệt bo cổ thoáng khí'),
(N'Áo Sơ Mi Nam Công Sở', N'Áo sơ mi dài tay chống nhăn, phom đứng lịch thiệp'),
(N'Áo Khoác Gió Nam', N'Áo khoác 2 lớp cản gió, kháng nước nhẹ'),
(N'Áo Hoodie & Sweater Nam', N'Áo nỉ ấm áp, thời trang Thu Đông trẻ trung');

-- 4.3. Tài khoản Quản trị viên & Nhân sự CRM
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('admin', '123456', 3, 1, GETDATE()),
('manager01', '123456', 1, 1, GETDATE()),
('sales01', '123456', 1, 1, GETDATE()),
('kho01', '123456', 1, 1, GETDATE()),
('crm01', '123456', 1, 1, GETDATE());

INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, NgaySinh, GioiTinh, ChucVu, TrangThai) VALUES 
(1, N'Quản Trị Viên Hệ Thống', 'admin@fashionstore.com', '0901234567', NULL, NULL, N'Admin', 1),
(2, N'Trần Đức Long', 'manager@fashionstore.com', '0901234001', '1990-04-12', N'Nam', N'Quản lý Cửa Hàng', 1),
(3, N'Lê Thị Mai', 'sales@fashionstore.com', '0901234002', '1995-08-20', N'Nữ', N'Nhân viên Bán Hàng', 1),
(4, N'Nguyễn Văn Hùng', 'kho@fashionstore.com', '0901234003', '1992-11-05', N'Nam', N'Nhân viên Kho', 1),
(5, N'Phạm Thu Hà', 'cskh@fashionstore.com', '0901234004', '1996-03-15', N'Nữ', N'Nhân viên Chăm Sóc Khách Hàng', 1);

-- 4.4. Khởi tạo Khách hàng (Dùng Cursor để tự động sinh Data)
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
('khachhang19', N'Đoàn Văn Vũ', 'vu.doan@gmail.com', '0932109876', '2001-09-14', N'Số 18 Võ Văn Kiệt, Quận 5, TP.HCM', 0); 

DECLARE @tTen VARCHAR(50), @tTenHT NVARCHAR(100), @tEmail VARCHAR(100), @tSDT VARCHAR(15), @tNS DATE, @tDC NVARCHAR(255), @tTT BIT;
DECLARE @curMaTK INT;

DECLARE c CURSOR FOR SELECT TenDN, HoTen, Email, SDT, NgaySinh, DiaChi, TrangThai FROM @KhachHangTable;
OPEN c;
FETCH NEXT FROM c INTO @tTen, @tTenHT, @tEmail, @tSDT, @tNS, @tDC, @tTT;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao)
    VALUES (@tTen, '12345678', 2, @tTT, GETDATE());
    SET @curMaTK = SCOPE_IDENTITY();

    INSERT INTO KHACHHANG (MaTK, HoTen, Email, SoDienThoai, NgaySinh, GioiTinh, DiaChi, NgayDangKy, DaXoa)
    VALUES (@curMaTK, @tTenHT, @tEmail, @tSDT, @tNS, N'Nam', @tDC, GETDATE(), 0);

    FETCH NEXT FROM c INTO @tTen, @tTenHT, @tEmail, @tSDT, @tNS, @tDC, @tTT;
END;
CLOSE c;
DEALLOCATE c;

-- 4.5. Gán sở thích
INSERT INTO KHACHHANG_SOTHICH (MaKH, MaDM) VALUES
(1, 1), (1, 2), (1, 5), (2, 2), (2, 3), (2, 4), (3, 1), (3, 5), (3, 4), (4, 1), (4, 4), (4, 5),
(5, 3), (5, 4), (5, 2), (6, 1), (6, 4), (6, 2), (7, 2), (7, 3), (7, 4), (8, 1), (8, 5), (8, 4),
(9, 3), (9, 4), (9, 2), (10, 2), (10, 5), (10, 1), (11, 3), (11, 4), (11, 2), (12, 2), (12, 4), (12, 1),
(13, 1), (13, 5), (13, 4), (14, 1), (14, 4), (14, 2), (15, 3), (15, 4), (15, 2), (16, 2), (16, 3), (16, 5),
(17, 3), (17, 4), (17, 2), (18, 1), (18, 5), (18, 4), (19, 1), (19, 2), (19, 5);


-- 4.6. Thêm 36 Sản phẩm (Thay thế cho các cục insert bị rời rạc)
INSERT INTO SANPHAM (MaDM, TenSP, ChatLieu, MoTa, TrangThai) VALUES
-- Áo Thun Nam Oversize (8 sp)
(1, N'Áo Thun Nam Cotton 100% Basic Minimalist Atino', N'Cotton Compact 100% 220gsm', N'Áo phông trơn tối giản, phom suông năng động mặc hàng ngày', 1),
(1, N'Áo Thun Nam Oversize In Họa Tiết Graphic Urban', N'Cotton 2 chiều 250gsm', N'Thiết kế in typo đường phố cá tính, thấm hút mồ hôi tốt', 1),
(1, N'Áo Thun Nam Cổ Tròn Phối Viền Raglan Retro', N'Cotton Spandex co giãn', N'Phong cách vintage thể thao, bo cổ dệt dặn dặn giữ form', 1),
(1, N'Áo Thun Nam Tay Lỡ Form Rộng Washed Vintage', N'Cotton Washed xử lý mềm', N'Hiệu ứng wash loang xám thời thượng, phong cách streetwear', 1),
(1, N'Áo Thun Nam Cổ Tim Trơn Thoáng Khí Hè', N'Cotton pha Modal siêu mịn', N'Áo thun cổ tim trẻ trung, co giãn nhẹ và tôn dáng ngực', 1),
(1, N'Áo Thun Nam Cotton Dry-Touch Kháng Khuẩn', N'Cotton công nghệ Dry-Touch', N'Chất vải dệt hạt mè thoáng khí, thoát ẩm nhanh khi vận động', 1),
(1, N'Áo Thun Nam Oversize Phối Túi Hộp Trước Ngực', N'Cotton 100% cao cấp', N'Điểm nhấn túi nắp tiện lợi, phong cách casual hiện đại', 1),
(1, N'Áo Thun Nam Trơn Dáng Rộng Drop-Shoulder', N'Cotton Heavyweight 260gsm', N'Thiết kế vai trễ phong cách Hàn Quốc, dày dặn đứng form', 1),

-- Áo Polo Nam Dệt Tổ Ong (8 sp)
(2, N'Áo Polo Nam Dệt Tổ Ong Phối Bo Dệt Kháng Khuẩn', N'CVC dệt tổ ong 230gsm', N'Thoáng khí vượt trội, cổ dệt chống quăn mép sang trọng', 1),
(2, N'Áo Polo Nam Cổ Bẻ Khóa Kéo Zip Kim Loại', N'Cotton pha Poly dệt Pique', N'Thiết kế cổ khóa kéo hiện đại, thanh lịch nơi công sở', 1),
(2, N'Áo Polo Nam Form Regular Thêu Logo Ngực Tinh Tế', N'Cotton Spandex 4 chiều', N'Co giãn thoải mái, màu sắc trung tính dễ phối đồ', 1),
(2, N'Áo Polo Nam Phối Màu Color Block Trẻ Trung', N'Vải dệt cá sấu mắt chim', N'Phối line màu nổi bật ở cổ và bo tay, phong cách năng động', 1),
(2, N'Áo Polo Nam Cổ Bo Trơn Nẹp Cúc Ẩn Tối Giản', N'Sợi Cafe khử mùi tự nhiên', N'Chất liệu sợi cafe thân thiện môi trường, chống tia UV', 1),
(2, N'Áo Polo Nam Họa Tiết Kẻ Ngang Thủy Thủ', N'Cotton dệt Jacquard', N'Họa tiết sọc ngang dệt chìm sang trọng, không bai dão', 1),
(2, N'Áo Polo Nam Thể Thao Dáng Ôm Vừa Slim-Fit', N'Poly Coolmax thể thao', N'Vải thể thao lạnh, co giãn đa chiều thích hợp chơi golf và dạo phố', 1),
(2, N'Áo Polo Nam Dệt Waffle Cổ V Không Cúc', N'Cotton dệt tổ ong Waffle', N'Cổ chữ V phóng khoáng phong cách resort Địa Trung Hải', 1),

-- Áo Sơ Mi Nam Công Sở (8 sp)
(3, N'Áo Sơ Mi Nam Dài Tay Cổ Đức Trắng Chống Nhăn', N'Vải sợi Bamboo (Sợi tre)', N'Chống nhăn tự nhiên, mềm mát kháng khuẩn, phom đứng chuẩn công sở', 1),
(3, N'Áo Sơ Mi Nam Ngắn Tay Cổ Bẻ Xanh Pastel Đi Làm', N'Cotton pha Silk cao cấp', N'Thoáng mát nhẹ tênh mùa hè, màu xanh nhạt lịch lãm', 1),
(3, N'Áo Sơ Mi Nam Cổ Tàu Kháng Nhăn Dáng Suông', N'Vải Modal dệt vân chìm', N'Cổ trụ thanh lịch trẻ trung, phù hợp cả đi làm và đi tiệc nhẹ', 1),
(3, N'Áo Sơ Mi Nam Họa Tiết Kẻ Caro Nhỏ Business Casual', N'Cotton Oxford dệt dày dặn', N'Kẻ caro nhã nhặn, chất Oxford đứng form cá tính', 1),
(3, N'Áo Sơ Mi Nam Dài Tay Kẻ Sọc Tăm Thanh Mảnh', N'Cotton Poplin cao cấp', N'Họa tiết kẻ sọc dọc giúp tôn dáng thon gọn, lịch sự', 1),
(3, N'Áo Sơ Mi Nam Denim Cotton Dài Tay Phong Trần', N'Chambray Denim mỏng nhẹ', N'Chất vải giả jean mềm mại, tạo phong cách nam tính bụi bặm', 1),
(3, N'Áo Sơ Mi Nam Vải Đũi Linen Ngắn Tay Mát Mẻ', N'Linen pha Cotton tự nhiên', N'Thấm hút mồ hôi tối đa, mộc mạc và thoải mái ngày hè', 1),
(3, N'Áo Sơ Mi Nam Công Sở Cổ Cutaway Hiện Đại', N'Vải Nano thế hệ mới', N'Cổ áo vát góc hiện đại dễ thắt cà vạt, giữ nếp suốt ngày dài', 1),

-- Áo Khoác Gió Nam (6 sp)
(4, N'Áo Khoác Gió Nam 2 Lớp Kháng Nước Cản Gió Atino', N'Polyester Microfiber tráng PU', N'Chống thấm nước mưa nhỏ, cản gió giữ ấm và khóa kéo ép nhiệt', 1),
(4, N'Áo Khoác Gió Bomber Nam Phối Bo Cổ Cổ Điển', N'Gió gân chống nhăn lót lụa', N'Dáng áo bomber nam tính, bo chun co giãn ôm vừa vặn', 1),
(4, N'Áo Khoác Gió Nam Có Mũ Tháo Rời Tiện Lợi', N'Vải dù gió trượt nước', N'Mũ trùm đầu tháo rời linh hoạt, có túi khóa zip trong tiện dụng', 1),
(4, N'Áo Khoác Gió Thể Thao Phản Quang Chạy Bộ', N'Nylon Ripstop siêu nhẹ', N'Vải gió siêu nhẹ gấp gọn thành túi nhỏ, viền phản quang ban đêm', 1),
(4, N'Áo Khoác Gió Nam Phối Màu 3 Khối Outdoor', N'Polyester trượt nước 2 lớp', N'Phong cách dã ngoại năng động, cổ cao chắn gió tuyệt đối', 1),
(4, N'Áo Khoác Gió Nam Lót Lưới Thoáng Khí Đi Phượt', N'Vải gió chống xước lót lưới', N'Lớp lưới bên trong chống dính mồ hôi, thông thoáng khi di chuyển', 1),

-- Áo Hoodie & Sweater Nam (6 sp)
(5, N'Áo Hoodie Nam Nỉ Bông Có Mũ Trơn Dáng Rộng', N'Nỉ chân cua 350gsm lót bông', N'Áo hoodie giữ ấm tốt, mũ 2 lớp dày dặn phối dây rút kim loại', 1),
(5, N'Áo Sweater Nam Cổ Tròn Thêu Chữ Trước Ngực', N'Nỉ da cá Cotton 100%', N'Chất nỉ mềm không xù lông, bo cổ tay co giãn giữ ấm hiệu quả', 1),
(5, N'Áo Hoodie Nam Khóa Kéo Zip-Up 2 Chiều', N'Nỉ chần bông mịn', N'Khóa zip 2 đầu tiện lợi mặc khoác ngoài phong cách Streetwear', 1),
(5, N'Áo Sweater Nam Dệt Len Gân Cổ Lọ 3cm Thu Đông', N'Sợi len Acrylic pha Cotton', N'Cổ lọ 3 phân ấm áp lịch lãm, phối hoàn hảo cùng áo dạ và blazer', 1),
(5, N'Áo Hoodie Nam Form Oversize In Graphic Lưng', N'Nỉ bông dày dặn đứng phom', N'Họa tiết nghệ thuật in nổi phía sau lưng, trẻ trung phá cách', 1),
(5, N'Áo Sweater Nam Phối Màu Cổ Bẻ Phong Cách Polo', N'Nỉ da cá phối cổ sơ mi', N'Thiết kế lai giữa áo nỉ và polo, mang lại vẻ ngoài thư sinh thanh lịch', 1);

-- Tự động điền đường dẫn ảnh cho toàn bộ 36 Sản phẩm
DECLARE @i INT = 1;
WHILE @i <= 36
BEGIN
    UPDATE SANPHAM
    SET HinhAnh = '/images/products/sp' + CAST(@i AS VARCHAR(5)) + '_thumb.jpg',
        HinhAnhMoTa = '/images/products/sp' + CAST(@i AS VARCHAR(5)) + '_detail.jpg'
    WHERE MaSP = @i;
    SET @i = @i + 1;
END;

-- 4.7. Tự động sinh Size, Màu, Tồn kho & Giá vào bảng SOLUONGSP cho 36 sản phẩm
INSERT INTO SOLUONGSP (MaSP, SKU, KichThuoc, MauSac, GiaBan, SoLuongTon, TrangThai, HinhAnh)
SELECT 
    sp.MaSP,
    CONCAT('SKU-SP', sp.MaSP, '-', v.Size),
    v.Size,
    v.Color,
    CASE 
        WHEN sp.MaDM = 1 THEN 220000 
        WHEN sp.MaDM = 2 THEN 320000 
        WHEN sp.MaDM = 3 THEN 380000 
        WHEN sp.MaDM = 4 THEN 450000 
        ELSE 390000                  
    END AS GiaBan,
    50 AS SoLuongTon, 
    1 AS TrangThai,
    sp.HinhAnh
FROM SANPHAM sp
CROSS JOIN (
    VALUES (N'M', N'Đen'), (N'L', N'Trắng'), (N'XL', N'Xanh Navy')
) AS v(Size, Color);

-- 4.8. Khởi tạo bài Khảo sát
DECLARE @MaKS1 INT, @MaKS2 INT;
DECLARE @MaCH1 INT, @MaCH2 INT, @MaCH3 INT;

-- KS1: Áo Polo nam
INSERT INTO KHAOSAT (MaQL, TenKhaoSat, MoTa, NgayBatDau, NgayKetThuc, TrangThai)
VALUES (5, N'Khảo sát Mẫu Áo Polo Nam Dệt Tổ Ong Thu Đông 2026', N'Khảo sát chất liệu và mức giá.', '2026-09-20T00:00:00', '2026-10-31T23:59:00', 1);
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

-- KS2: Áo sơ mi nam
INSERT INTO KHAOSAT (MaQL, TenKhaoSat, MoTa, NgayBatDau, NgayKetThuc, TrangThai)
VALUES (5, N'Khảo sát Nhu cầu Dòng Áo Sơ Mi Nam Cổ Tàu Kháng Nhăn', N'Thăm dò ý kiến.', '2026-09-22T00:00:00', '2026-11-15T23:59:00', 1);
SET @MaKS2 = SCOPE_IDENTITY();

INSERT INTO CAUHOI_KHAOSAT (MaKS, NoiDung, LoaiCauHoi, ThuTu, BatBuoc) VALUES 
(@MaKS2, N'Tính năng nào bạn đánh giá cao nhất ở sơ mi nam Atino?', N'Một lựa chọn', 1, 1);
SET @MaCH3 = SCOPE_IDENTITY();
INSERT INTO LUACHON_CAUHOI (MaCauHoi, NoiDung, ThuTu) VALUES
(@MaCH3, N'Khả năng chống nhăn / hạn chế nhàu vải', 1),
(@MaCH3, N'Chất vải thoáng mát thấm hút mồ hôi', 2),
(@MaCH3, N'Cổ áo đứng phom lịch thiệp', 3);

-- Phát phiếu khảo sát
INSERT INTO PHIEUKHAOSAT (MaKS, MaKH, NgayBatDau, NgayNop, TrangThai) VALUES
(@MaKS1, 1, '2026-09-22', '2026-09-22', N'Đã hoàn thành'),
(@MaKS1, 2, '2026-09-23', '2026-09-23', N'Đã hoàn thành'),
(@MaKS1, 3, '2026-09-24', NULL, N'Đang làm');

-- 4.10. Phản hồi & Đánh giá mẫu
INSERT INTO PHANHOI (MaKH, MaSP, MaQL, TieuDe, NoiDung, LoaiPhanHoi, SoSao, TrangThai, PhanHoiQuanLy, NgayGui) VALUES
(1, 1, 5, N'Đánh giá áo thun', N'Form chuẩn oversize, chất cotton mát.', N'Chất lượng', 5, N'Đã xử lý', N'Cảm ơn bạn An!', '2026-09-22'),
(2, 3, NULL, N'Góp ý áo sơ mi', N'Vải áo mềm mịn nhưng cổ tay áo hơi rộng.', N'Form dáng', 4, N'Chưa xử lý', NULL, '2026-09-24'),
(3, 2, NULL, N'Hỏi về áo Polo', N'Áo mặc đẹp nhưng sau 2 lần giặt màu vải hơi bị xỉn nhẹ.', N'Chất lượng', 3, N'Chưa xử lý', NULL, '2026-09-25');

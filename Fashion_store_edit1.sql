
CREATE DATABASE Fashion_store;
GO
USE Fashion_store;
GO

-- =======================================================
-- 1. PHÂN HỆ TÀI KHOẢN & NGƯỜI DÙNG
-- =======================================================

-- Bảng Vai trò người dùng (Admin, Quản lý, Khách hàng)
CREATE TABLE VAITRO
(
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    MoTa NVARCHAR(255)
);
GO

-- Bảng Tài khoản đăng nhập
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

-- Bảng Hồ sơ Quản lý
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

-- Bảng Hồ sơ Khách hàng (CRM)
CREATE TABLE KHACHHANG
(
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    MaTK INT NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15),
    NgaySinh DATE,                                -- Thống kê tỷ lệ độ tuổi trong CRM
    GioiTinh NVARCHAR(10),
    DiaChi NVARCHAR(255),
    SoThich NVARCHAR(500),                        -- Thống kê sở thích trong CRM
    NgayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
    DaXoa BIT NOT NULL DEFAULT 0,                 -- Xóa mềm: 0 là còn hoạt động, 1 là đã xóa

    CONSTRAINT FK_KHACHHANG_TAIKHOAN
        FOREIGN KEY (MaTK)
        REFERENCES TAIKHOAN(MaTK)
);
GO

-- =======================================================
-- 2. PHÂN HỆ DANH MỤC SẢN PHẨM
-- =======================================================

-- Bảng Danh mục sản phẩm (Áo thun, Sơ mi, Quần Jean, Váy...)
CREATE TABLE DANHMUC
(
    MaDM INT IDENTITY(1,1) PRIMARY KEY,
    TenDM NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(500),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

-- =======================================================
-- 3. PHÂN HỆ SẢN PHẨM & BIẾN THỂ (SIZE / MÀU)
-- =======================================================

-- Bảng Sản phẩm (Lưu thông tin chung của mẫu thiết kế)
CREATE TABLE SANPHAM
(
    MaSP INT IDENTITY(1,1) PRIMARY KEY,
    MaDM INT NOT NULL,
    TenSP NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    ChatLieu NVARCHAR(100),                       -- Cotton, Kaki, Lụa, Denim...
    TrangThai BIT NOT NULL DEFAULT 1,             -- 1: Đang bán, 0: Ẩn
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_SANPHAM_DANHMUC
        FOREIGN KEY (MaDM)
        REFERENCES DANHMUC(MaDM)
);
GO

-- Bảng Số lượng sản phẩm (Quản lý Biến thể: Size, Màu, Tồn kho, Giá bán)
CREATE TABLE SOLUONGSP
(
    MaSL INT IDENTITY(1,1) PRIMARY KEY,
    MaSP INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,              -- Mã định danh riêng biệt từng loại
    KichThuoc NVARCHAR(20) NOT NULL,              -- S, M, L, XL, 29, 30...
    MauSac NVARCHAR(50) NOT NULL,                 -- Đen, Trắng, Xám, Be...
    GiaBan DECIMAL(18,2) NOT NULL,
    SoLuongTon INT NOT NULL DEFAULT 0,
    HinhAnh NVARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_SOLUONGSP_SANPHAM
        FOREIGN KEY (MaSP)
        REFERENCES SANPHAM(MaSP)
        ON DELETE CASCADE,

    CONSTRAINT CK_SOLUONGSP_GIA
        CHECK (GiaBan >= 0),

    CONSTRAINT CK_SOLUONGSP_SOLUONG
        CHECK (SoLuongTon >= 0)
);
GO

-- =======================================================
-- 4. PHÂN HỆ ĐƠN HÀNG & CHI TIẾT ĐƠN HÀNG
-- =======================================================

-- Bảng Đơn đặt hàng
CREATE TABLE DONHANG
(
    MaDH INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaQLXuLy INT NULL,                            -- Quản lý tiếp nhận/xử lý đơn
    NgayDat DATETIME NOT NULL DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận',
    DiaChiGiaoHang NVARCHAR(255) NOT NULL,
    SoDienThoaiNhan VARCHAR(15),
    GhiChu NVARCHAR(500) NULL,                    -- Ghi chú giao hàng của khách

    CONSTRAINT FK_DONHANG_KHACHHANG
        FOREIGN KEY (MaKH)
        REFERENCES KHACHHANG(MaKH),

    CONSTRAINT FK_DONHANG_QUANLY
        FOREIGN KEY (MaQLXuLy)
        REFERENCES QUANLY(MaQL),

    CONSTRAINT CK_DONHANG_TONGTIEN
        CHECK (TongTien >= 0),

    CONSTRAINT CK_DONHANG_TRANGTHAI
        CHECK
        (
            TrangThai IN
            (
                N'Chờ xác nhận',
                N'Đã xác nhận',
                N'Đang giao',
                N'Đã giao',
                N'Đã hủy'
            )
        )
);
GO

-- Bảng Chi tiết đơn hàng ( Show lịch sử, chi tiết đơn hàng)
CREATE TABLE CHITIETDONHANG
(
    MaDH INT NOT NULL,
    MaSL INT NOT NULL,                            -- Liên kết tới biến thể SOLUONGSP

    SoLuong INT NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL,                -- Đơn giá tại thời điểm mua
    GiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,     -- Tiền chiết khấu / giảm giá nếu có

    -- Cột tính toán tự động: (Số lượng * Đơn giá) - Giảm giá
    ThanhTien AS ((SoLuong * DonGia) - GiamGia) PERSISTED,

    CONSTRAINT PK_CHITIETDONHANG
        PRIMARY KEY (MaDH, MaSL),

    CONSTRAINT FK_CTDH_DONHANG
        FOREIGN KEY (MaDH)
        REFERENCES DONHANG(MaDH)
        ON DELETE CASCADE,

    CONSTRAINT FK_CTDH_SOLUONGSP
        FOREIGN KEY (MaSL)
        REFERENCES SOLUONGSP(MaSL),

    CONSTRAINT CK_CTDH_SOLUONG
        CHECK (SoLuong > 0),

    CONSTRAINT CK_CTDH_DONGIA
        CHECK (DonGia >= 0),

    CONSTRAINT CK_CTDH_GIAMGIA
        CHECK (GiamGia >= 0)
);
GO

-- =======================================================
-- 5. PHÂN HỆ PHẢN HỒI & ĐÁNH GIÁ (CRM)
-- =======================================================

-- Bảng Phản hồi / Khiếu nại của khách hàng
CREATE TABLE PHANHOI
(
    MaPH INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaSP INT NULL,                                -- Khiếu nại theo sản phẩm cụ thể (nếu có)
    MaQL INT NULL,                                -- Quản lý chịu trách nhiệm giải quyết
    TieuDe NVARCHAR(200),
    NoiDung NVARCHAR(MAX) NOT NULL,
    LoaiPhanHoi NVARCHAR(50),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chưa xử lý',
    PhanHoiQuanLy NVARCHAR(MAX),
    NgayGui DATETIME NOT NULL DEFAULT GETDATE(),
    NgayXuLy DATETIME NULL,

    CONSTRAINT FK_PHANHOI_KHACHHANG
        FOREIGN KEY (MaKH)
        REFERENCES KHACHHANG(MaKH),

    CONSTRAINT FK_PHANHOI_SANPHAM
        FOREIGN KEY (MaSP)
        REFERENCES SANPHAM(MaSP),

    CONSTRAINT FK_PHANHOI_QUANLY
        FOREIGN KEY (MaQL)
        REFERENCES QUANLY(MaQL),

    CONSTRAINT CK_PHANHOI_TRANGTHAI
        CHECK
        (
            TrangThai IN
            (
                N'Chưa xử lý',
                N'Đang xử lý',
                N'Đã xử lý',
                N'Đã đóng'
            )
        )
);
GO

-- Bảng Đánh giá sản phẩm
CREATE TABLE DANHGIA
(
    MaDG INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL,
    MaSP INT NOT NULL,
    SoSao INT NOT NULL,                           -- Đánh giá 1 đến 5 sao
    NoiDung NVARCHAR(1000),
    NgayDanhGia DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_DANHGIA_KHACHHANG
        FOREIGN KEY (MaKH)
        REFERENCES KHACHHANG(MaKH),

    CONSTRAINT FK_DANHGIA_SANPHAM
        FOREIGN KEY (MaSP)
        REFERENCES SANPHAM(MaSP),

    CONSTRAINT CK_DANHGIA_SOSAO
        CHECK (SoSao BETWEEN 1 AND 5),

    CONSTRAINT UQ_DANHGIA_KH_SP
        UNIQUE (MaKH, MaSP)                       -- Mỗi khách chỉ đánh giá 1 sản phẩm 1 lần
);
GO

-- =======================================================
-- 6. PHÂN HỆ KHẢO SÁT Ý KIẾN KHÁCH HÀNG (CRM)
-- =======================================================

-- Bảng Chiến dịch khảo sát
CREATE TABLE KHAOSAT
(
    MaKS INT IDENTITY(1,1) PRIMARY KEY,
    MaQL INT NOT NULL,
    TenKhaoSat NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NOT NULL,
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_KHAOSAT_QUANLY
        FOREIGN KEY (MaQL)
        REFERENCES QUANLY(MaQL),

    CONSTRAINT CK_KHAOSAT_NGAY
        CHECK (NgayKetThuc >= NgayBatDau)
);
GO

-- Bảng Câu hỏi khảo sát
CREATE TABLE CAUHOI_KHAOSAT
(
    MaCauHoi INT IDENTITY(1,1) PRIMARY KEY,
    MaKS INT NOT NULL,
    NoiDung NVARCHAR(1000) NOT NULL,
    LoaiCauHoi NVARCHAR(30) NOT NULL,             -- Một lựa chọn, Nhiều lựa chọn, Tự luận, Đánh giá sao
    ThuTu INT NOT NULL DEFAULT 1,
    BatBuoc BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_CAUHOI_KHAOSAT
        FOREIGN KEY (MaKS)
        REFERENCES KHAOSAT(MaKS),

    CONSTRAINT CK_CAUHOI_LOAI
        CHECK
        (
            LoaiCauHoi IN
            (
                N'Một lựa chọn',
                N'Nhiều lựa chọn',
                N'Tự luận',
                N'Đánh giá sao'
            )
        )
);
GO

-- Bảng Các lựa chọn trả lời cho câu hỏi trắc nghiệm
CREATE TABLE LUACHON_CAUHOI
(
    MaLuaChon INT IDENTITY(1,1) PRIMARY KEY,
    MaCauHoi INT NOT NULL,
    NoiDung NVARCHAR(500) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_LUACHON_CAUHOI
        FOREIGN KEY (MaCauHoi)
        REFERENCES CAUHOI_KHAOSAT(MaCauHoi)
);
GO

-- Bảng Phiếu khảo sát của khách hàng
CREATE TABLE PHIEUKHAOSAT
(
    MaPhieuKS INT IDENTITY(1,1) PRIMARY KEY,
    MaKS INT NOT NULL,
    MaKH INT NOT NULL,
    NgayBatDau DATETIME NOT NULL DEFAULT GETDATE(),
    NgayNop DATETIME NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Đang làm',

    CONSTRAINT FK_PHIEUKS_KHAOSAT
        FOREIGN KEY (MaKS)
        REFERENCES KHAOSAT(MaKS),

    CONSTRAINT FK_PHIEUKS_KHACHHANG
        FOREIGN KEY (MaKH)
        REFERENCES KHACHHANG(MaKH),

    CONSTRAINT CK_PHIEUKS_TRANGTHAI
        CHECK
        (
            TrangThai IN
            (
                N'Đang làm',
                N'Đã hoàn thành'
            )
        ),

    CONSTRAINT UQ_PHIEUKS_KH_KS
        UNIQUE (MaKS, MaKH)
);
GO

-- Bảng Câu trả lời chi tiết của khách hàng
CREATE TABLE TRALOI_KHAOSAT
(
    MaTraLoi INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuKS INT NOT NULL,
    MaCauHoi INT NOT NULL,
    MaLuaChon INT NULL,
    NoiDungTraLoi NVARCHAR(MAX),
    SoSao INT NULL,

    CONSTRAINT FK_TRALOI_PHIEUKS
        FOREIGN KEY (MaPhieuKS)
        REFERENCES PHIEUKHAOSAT(MaPhieuKS),

    CONSTRAINT FK_TRALOI_CAUHOI
        FOREIGN KEY (MaCauHoi)
        REFERENCES CAUHOI_KHAOSAT(MaCauHoi),

    CONSTRAINT FK_TRALOI_LUACHON
        FOREIGN KEY (MaLuaChon)
        REFERENCES LUACHON_CAUHOI(MaLuaChon),

    CONSTRAINT CK_TRALOI_SOSAO
        CHECK
        (
            SoSao IS NULL OR SoSao BETWEEN 1 AND 5
        )
);
GO
-- 1. Thêm Vai trò (MaVaiTro tự động tăng 1, 2)
INSERT INTO VAITRO (TenVaiTro, MoTa) VALUES 
(N'Quản lý', N'Quản trị viên / Admin hệ thống'),
(N'Khách hàng', N'Người dùng mua hàng');

-- 2. Thêm Danh mục mẫu
INSERT INTO DANHMUC (TenDM, MoTa) VALUES 
(N'Áo sơ mi', N'Áo sơ mi nam công sở, dạo phố'),
(N'Áo thun', N'Áo thun cotton cổ tròn, cổ tim'),
(N'Áo Polo', N'Áo Polo nam lịch sự');

-- 3. Thêm Tài khoản Quản trị viên gốc (Admin)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('admin', '123456', 1, 1, GETDATE());
-- 4. Thêm Hồ sơ Quản lý cho Admin (MaTK = 1)
INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(1, N'Quản Trị Viên Hệ Thống', 'admin@fashionstore.com', '0901234567', N'Admin', 1);

-- 5. Thêm các tài khoản Nhân sự nội bộ mẫu (4 vị trí phân quyền)
-- 5.1 Quản lý Cửa Hàng (Store Manager)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('manager01', '123456', 1, 1, GETDATE());
INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(2, N'Trần Đức Long', 'manager@fashionstore.com', '0901234001', N'Quản lý Cửa Hàng', 1);

-- 5.2 Nhân viên Bán Hàng (Sales Staff)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('sales01', '123456', 1, 1, GETDATE());
INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(3, N'Lê Thị Mai', 'sales@fashionstore.com', '0901234002', N'Nhân viên Bán Hàng', 1);

-- 5.3 Nhân viên Kho (Warehouse Staff)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('kho01', '123456', 1, 1, GETDATE());
INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(4, N'Nguyễn Văn Hùng', 'kho@fashionstore.com', '0901234003', N'Nhân viên Kho', 1);

-- 5.4 Nhân viên Chăm Sóc Khách Hàng (CRM Staff)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('crm01', '123456', 1, 1, GETDATE());
INSERT INTO QUANLY (MaTK, HoTen, Email, SoDienThoai, ChucVu, TrangThai) VALUES 
(5, N'Phạm Thu Hà', 'cskh@fashionstore.com', '0901234004', N'Nhân viên Chăm Sóc Khách Hàng', 1);

-- 6. Thêm các tài khoản Khách Hàng mẫu (CRM)
-- 6.1 Khách hàng 01
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('khachhang01', '123456', 2, 1, GETDATE());
INSERT INTO KHACHHANG (MaTK, HoTen, Email, SoDienThoai, NgaySinh, GioiTinh, DiaChi, SoThich, NgayDangKy, DaXoa) VALUES 
(6, N'Nguyễn Văn An', 'an.nguyen@gmail.com', '0912345678', '1998-05-15', N'Nam', N'Số 45 Cầu Giấy, Hà Nội', N'Áo polo, phong cách công sở, tone màu tối', GETDATE(), 0);

-- 6.2 Khách hàng 02
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaVaiTro, TrangThai, NgayTao) VALUES 
('khachhang02', '123456', 2, 1, GETDATE());
INSERT INTO KHACHHANG (MaTK, HoTen, Email, SoDienThoai, NgaySinh, GioiTinh, DiaChi, SoThich, NgayDangKy, DaXoa) VALUES 
(7, N'Trần Thị Bích', 'bich.tran@gmail.com', '0987654321', '2001-10-20', N'Nữ', N'Số 12 Lê Lợi, TP.HCM', N'Váy đầm dạo phố, màu pastel, phụ kiện thanh lịch', GETDATE(), 0);
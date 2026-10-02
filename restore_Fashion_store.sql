-- ==============================================================================
-- BỘ SCRIPT KHÔI PHỤC (RESTORE) CƠ SỞ DỮ LIỆU SQL SERVER
-- CSDL: Fashion_store
-- Đồ án: Hệ Thống Thông Tin Doanh Nghiệp (HTTTDN)
-- ==============================================================================

USE master;
GO

-- ------------------------------------------------------------------------------
-- KỊCH BẢN 1: KHÔI PHỤC CƠ BẢN TỪ BẢN FULL BACKUP (GHI ĐÈ CSDL CŨ)
-- Sử dụng khi CSDL bị lỗi nặng hoặc muốn trả dữ liệu về mốc Full Backup.
-- ------------------------------------------------------------------------------
PRINT N'==============================================================================';
PRINT N'KỊCH BẢN 1: KHÔI PHỤC TỪ BẢN SAO LƯU TOÀN BỘ (FULL BACKUP)';
PRINT N'==============================================================================';
GO

-- 1. Ngắt toàn bộ kết nối hiện tại đến CSDL Fashion_store (nếu CSDL đang tồn tại)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'Fashion_store')
BEGIN
    PRINT N'==> Ngắt kết nối người dùng hiện tại và chuyển CSDL sang SINGLE_USER mode...';
    ALTER DATABASE Fashion_store SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
GO

-- 2. Khôi phục từ file Full Backup
PRINT N'==> Thực hiện khôi phục Full Backup với tùy chọn REPLACE...';
RESTORE DATABASE Fashion_store
FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak'
WITH 
    REPLACE,                 -- Ghi đè lên CSDL cũ nếu đã tồn tại
    RECOVERY,                -- Sẵn sàng cho người dùng truy vấn ngay sau khi xong
    STATS = 10;
GO

-- 3. Đưa CSDL trở lại chế độ nhiều người dùng (MULTI_USER)
ALTER DATABASE Fashion_store SET MULTI_USER;
GO
PRINT N'==> Hoàn tất Kịch bản 1: Khôi phục Full Backup thành công!';
GO


-- ------------------------------------------------------------------------------
-- KỊCH BẢN 2: KHÔI PHỤC THỨ TỰ THEO CHUỖI (FULL -> DIFFERENTIAL -> TRANSACTION LOG)
-- Áp dụng để khôi phục tối đa dữ liệu đến thời điểm sự cố mới nhất xảy ra.
-- ------------------------------------------------------------------------------
PRINT N'==============================================================================';
PRINT N'KỊCH BẢN 2: KHÔI PHỤC THEO CHUỖI (FULL + DIFF + LOG BACKUP)';
PRINT N'==============================================================================';
GO

-- 1. Ngắt kết nối người dùng
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'Fashion_store')
BEGIN
    ALTER DATABASE Fashion_store SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
GO

-- 2. Bước 2.1: Khôi phục bản FULL BACKUP với NORECOVERY (Giữ CSDL ở trạng thái Restoring)
PRINT N'==> 2.1 Restore Full Backup (WITH NORECOVERY)...';
RESTORE DATABASE Fashion_store
FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak'
WITH 
    REPLACE, 
    NORECOVERY,              -- Cho phép khôi phục tiếp các bản backup sau
    STATS = 10;
GO

-- 3. Bước 2.2: Khôi phục bản DIFFERENTIAL BACKUP với NORECOVERY
PRINT N'==> 2.2 Restore Differential Backup (WITH NORECOVERY)...';
RESTORE DATABASE Fashion_store
FROM DISK = N'C:\SQL_Backup\Fashion_store_Diff.bak'
WITH 
    NORECOVERY,              -- Tiếp tục chờ bản Log Backup
    STATS = 10;
GO

-- 4. Bước 2.3: Khôi phục bản TRANSACTION LOG BACKUP với RECOVERY (Hoàn tất chuỗi)
PRINT N'==> 2.3 Restore Transaction Log Backup (WITH RECOVERY)...';
RESTORE LOG Fashion_store
FROM DISK = N'C:\SQL_Backup\Fashion_store_Log.trn'
WITH 
    RECOVERY,                -- Đánh dấu hoàn tất quá trình Restore, mở CSDL cho người dùng
    STATS = 10;
GO

-- 5. Trả lại chế độ MULTI_USER
ALTER DATABASE Fashion_store SET MULTI_USER;
GO
PRINT N'==> Hoàn tất Kịch bản 2: Khôi phục chuỗi (Full + Diff + Log) thành công!';
GO


-- ------------------------------------------------------------------------------
-- KỊCH BẢN 3: KHÔI PHỤC SANG MỘT CSDL MỚI HOẶC SANG MÁY KHÁC (SỬ DỤNG WITH MOVE)
-- Giúp khôi phục CSDL thành "Fashion_store_Test" hoặc chuyển file data/log sang đường dẫn mới.
-- ------------------------------------------------------------------------------
PRINT N'==============================================================================';
PRINT N'KỊCH BẢN 3: KHÔI PHỤC SANG CSDL MỚI (Fashion_store_Test) VỚI TÙY CHỌN WITH MOVE';
PRINT N'==============================================================================';
GO

-- 1. Xem tên Logical Name của tập tin dữ liệu (.mdf) và nhật ký (.ldf) trong bản sao lưu
PRINT N'==> Danh sách Logical Name trong file Backup:';
RESTORE FILELISTONLY FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak';
GO

-- 2. Tiến hành Restore sang Database mới với tên Fashion_store_Test
PRINT N'==> Khôi phục thành Fashion_store_Test...';
RESTORE DATABASE Fashion_store_Test
FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak'
WITH 
    MOVE N'Fashion_store' TO N'C:\SQL_Backup\Fashion_store_Test.mdf',
    MOVE N'Fashion_store_log' TO N'C:\SQL_Backup\Fashion_store_Test_log.ldf',
    REPLACE,
    RECOVERY,
    STATS = 10;
GO

PRINT N'==> Hoàn tất Kịch bản 3: Đã tạo CSDL Fashion_store_Test từ bản sao lưu!';
GO

PRINT N'==============================================================================';
PRINT N'TẤT CẢ CÁC KỊCH BẢN RESTORE ĐÃ ĐƯỢC CHUẨN BỊ VÀ KIỂM TRA THÀNH CÔNG!';
PRINT N'==============================================================================';

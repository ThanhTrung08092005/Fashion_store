-- ==============================================================================
-- BỘ SCRIPT SAO LƯU (BACKUP) CƠ SỞ DỮ LIỆU SQL SERVER
-- CSDL: Fashion_store
-- Đồ án: Hệ Thống Thông Tin Doanh Nghiệp (HTTTDN)
-- ==============================================================================

USE master;
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 1: KIỂM TRA VÀ CẤU HÌNH CHẾ ĐỘ KHÔI PHỤC (RECOVERY MODEL) SANG FULL
-- Chế độ FULL hỗ trợ đầy đủ Sao lưu Toàn bộ (Full), Vi sai (Diff) và Log Giao dịch
-- ------------------------------------------------------------------------------
PRINT N'==> 1. Kiểm tra và thiết lập Recovery Model FULL cho Fashion_store...';
ALTER DATABASE Fashion_store SET RECOVERY FULL;
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 2: TẠO THƯ MỤC LƯU TRỮ BACKUP BẰNG XP_CMDSHELL (NẾU CÓ QUYỀN) HOẶC SỬ DỤNG ĐƯỜNG DẪN C:\SQL_Backup\
-- Lưu ý: Đảm bảo thư mục C:\SQL_Backup\ đã được tạo trên máy chủ SQL Server.
-- ------------------------------------------------------------------------------
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;

-- Tạo thư mục sao lưu C:\SQL_Backup nếu chưa tồn tại
EXEC master..xp_cmdshell 'cmd.exe /c "if not exist C:\SQL_Backup mkdir C:\SQL_Backup"';
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 3: SCRIPT KỊCH BẢN 1 - SAO LƯU TOÀN BỘ (FULL BACKUP)
-- Mụcl đích: Lưu trữ toàn bộ dữ liệu, cấu trúc bảng, index, trigger, stored procedure.
-- ------------------------------------------------------------------------------
PRINT N'==> 2. Đang thực hiện FULL BACKUP...';
GO

DECLARE @FullBackupPath NVARCHAR(500);
SET @FullBackupPath = N'C:\SQL_Backup\Fashion_store_Full_' 
    + REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR(19), GETDATE(), 120), '-', ''), ':', ''), ' ', '_') 
    + '.bak';

-- Sao lưu vào file có gắn Timestamp động
BACKUP DATABASE Fashion_store
TO DISK = @FullBackupPath
WITH 
    FORMAT,                  -- Khởi tạo lại thiết bị sao lưu
    INIT,                    -- Ghi đè tập tin sao lưu cũ nếu trùng tên
    NAME = N'Fashion_store-Full Database Backup',
    DESCRIPTION = N'Bản sao lưu toàn bộ CSDL Fashion_store',
    COMPRESSION,             -- Nén file sao lưu giúp giảm dung lượng ổ đĩa
    STATS = 10;              -- Hiển thị tiến độ mỗi 10%
GO

-- Đồng thời tạo 1 bản Cố định tên để dễ Restore cho bài tập/đồ án
BACKUP DATABASE Fashion_store
TO DISK = N'C:\SQL_Backup\Fashion_store_Full.bak'
WITH FORMAT, INIT, NAME = N'Fashion_store Full Fixed Backup', COMPRESSION, STATS = 10;
GO

PRINT N'==> Hoàn tất Full Backup tại C:\SQL_Backup\Fashion_store_Full.bak';
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 4: SCRIPT KỊCH BẢN 2 - SAO LƯU VI SAI (DIFFERENTIAL BACKUP)
-- Mục đích: Chỉ sao lưu các thay đổi dữ liệu phát sinh từ sau bản Full Backup gần nhất.
-- ------------------------------------------------------------------------------
PRINT N'==> 3. Đang thực hiện DIFFERENTIAL BACKUP...';
GO

BACKUP DATABASE Fashion_store
TO DISK = N'C:\SQL_Backup\Fashion_store_Diff.bak'
WITH 
    DIFFERENTIAL,            -- Đánh dấu sao lưu vi sai
    FORMAT, 
    INIT, 
    NAME = N'Fashion_store Differential Backup',
    DESCRIPTION = N'Bản sao lưu vi sai CSDL Fashion_store',
    COMPRESSION,
    STATS = 10;
GO

PRINT N'==> Hoàn tất Differential Backup tại C:\SQL_Backup\Fashion_store_Diff.bak';
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 5: SCRIPT KỊCH BẢN 3 - SAO LƯU NHẬT KÝ GIAO DỊCH (TRANSACTION LOG BACKUP)
-- Mục đích: Ghi lại toàn bộ lịch sử giao dịch (Insert/Update/Delete) hỗ trợ Point-in-Time Recovery.
-- ------------------------------------------------------------------------------
PRINT N'==> 4. Đang thực hiện TRANSACTION LOG BACKUP...';
GO

BACKUP LOG Fashion_store
TO DISK = N'C:\SQL_Backup\Fashion_store_Log.trn'
WITH 
    FORMAT, 
    INIT, 
    NAME = N'Fashion_store Transaction Log Backup',
    DESCRIPTION = N'Bản sao lưu Transaction Log CSDL Fashion_store',
    COMPRESSION,
    STATS = 10;
GO

PRINT N'==> Hoàn tất Log Backup tại C:\SQL_Backup\Fashion_store_Log.trn';
GO

-- ------------------------------------------------------------------------------
-- BƯỚC 6: KIỂM TRA TÍNH TOÀN VẸN CỦA CÁC BẢN SAO LƯU
-- ------------------------------------------------------------------------------
PRINT N'==> 5. Kiểm tra tính hợp lệ của file Backup (RESTORE VERIFYONLY)...';

RESTORE VERIFYONLY FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak';
RESTORE HEADERONLY FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak';
RESTORE FILELISTONLY FROM DISK = N'C:\SQL_Backup\Fashion_store_Full.bak';
GO

PRINT N'==============================================================================';
PRINT N'TẤT CẢ CÁC BƯỚC SAO LƯU CSDL FASHION_STORE ĐÃ HOÀN THÀNH THÀNH CÔNG!';
PRINT N'==============================================================================';

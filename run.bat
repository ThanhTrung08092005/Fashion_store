@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
echo ========================================================
echo    DANG KHOI DONG HE THONG FASHION STORE VA CRM
echo ========================================================

echo [0/2] Kiem tra va giai phong port 5000, 5232 cu neu bi ket...
powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5000, 5232 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }" >nul 2>&1

echo [1/2] Dang khoi chay Backend API (Cong 5000 - Load DB cho User)...
start "FashionCRM_API (Port 5000)" dotnet run --project backend/FashionCRM.API/FashionCRM.API.csproj

echo [2/2] Dang khoi chay Web Admin MVC (Cong 5232 - Quan ly)...
start "Fashion_Store_Admin (Port 5232)" dotnet run --project Fashion_store.csproj

echo.
echo Dang cho ca 2 server khoi dong va ket noi Database...
set READY=0
for /l %%i in (1,1,25) do (
    if "!READY!"=="0" (
        for /f %%a in ('curl.exe -s -o NUL -w "%%{http_code}" http://localhost:5000/api/danhmuc') do set C5000=%%a
        for /f %%b in ('curl.exe -s -o NUL -w "%%{http_code}" http://localhost:5232/Admin/Account/Login') do set C5232=%%b
        if "!C5000!"=="200" if "!C5232!"=="200" (
            set READY=1
            echo.
            echo [OK] Ca 2 server da khoi dong va ket noi Database thanh cong! (%%i giay)
        ) else (
            <nul set /p =.
            timeout /t 1 >nul
        )
    )
)

echo.
echo Dang mo cac trang tren trinh duyet...
start http://localhost:5232/frontend/customer/home.html
start http://localhost:5232/Admin/Account/Login

echo.
echo ========================================================
echo   HE THONG DA CHAY THANH CONG!
echo   - User Home (Load DB):     http://localhost:5232/frontend/customer/home.html
echo   - Admin Quan Ly Tai Khoan: http://localhost:5232/Admin/Account/AccountManagement
echo   - Admin Quan Ly San Pham:  http://localhost:5232/Admin/Product/ProductManagement
echo   (Neu dung VS Code Live Server: http://127.0.0.1:5500/frontend/customer/home.html)
echo ========================================================
pause

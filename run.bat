@echo off
setlocal
chcp 65001 >nul
set "APP_ROOT=%~dp0"
pushd "%APP_ROOT%" || exit /b 1

echo ========================================================
echo    DANG BIEN DICH HE THONG FASHION STORE VA CRM
echo ========================================================

echo [1/3] Dang bien dich Web Admin MVC va Backend API...
dotnet build "%APP_ROOT%Fashion_store.slnx" --configuration Debug --maxcpucount:1
if errorlevel 1 goto build_failed

echo [2/3] Dang khoi chay Backend API (Cong 5000 - Load DB cho User)...
start "FashionCRM_API (Port 5000)" /D "%APP_ROOT%backend\FashionCRM.API" dotnet run --no-build --launch-profile http

echo [3/3] Dang khoi chay Web Admin MVC (Cong 5232 - Quan ly tai khoan)...
start "Fashion_Store_Admin (Port 5232)" /D "%APP_ROOT%" dotnet run --no-build --launch-profile http --project "%APP_ROOT%Fashion_store.csproj"

echo.
echo Dang cho cac server khoi dong va ket noi Database (4 giay)...
timeout /t 4 >nul

echo.
echo Dang mo cac trang tren trinh duyet...
start http://localhost:5232/frontend/customer/home.html
start http://localhost:5232/Admin/Account/Login

echo.
echo ========================================================
echo   HE THONG DA CHAY THANH CONG!
echo   - User Home (Load DB):     http://localhost:5232/frontend/customer/home.html
echo   - Dang ky khach hang:     http://localhost:5232/frontend/customer/register.html
echo   - Admin Quan Ly Tai Khoan: http://localhost:5232/Admin/Account/AccountManagement
echo   - Admin Quan Ly San Pham:  http://localhost:5232/Admin/Product/ProductManagement
echo ========================================================
pause
popd
exit /b 0

:build_failed
echo.
echo BIEN DICH THAT BAI. Hay doc loi phia tren; he thong chua duoc khoi dong.
pause
popd
exit /b 1

@echo off
chcp 65001 >nul
echo ========================================================
echo    DANG KHOI DONG HE THONG FASHION STORE VA CRM
echo ========================================================

echo [1/2] Dang khoi chay Backend API (Cong 5000 - Load DB cho User)...
start "FashionCRM_API (Port 5000)" dotnet run --project backend/FashionCRM.API/FashionCRM.API.csproj

echo [2/2] Dang khoi chay Web Admin MVC (Cong 5232 - Quan ly tai khoan)...
start "Fashion_Store_Admin (Port 5232)" dotnet run --project Fashion_store.csproj

echo.
echo Dang cho cac server khoi dong va ket noi Database (4 giay)...
timeout /t 4 >nul

echo.
echo Dang mo cac trang tren trinh duyet...
start http://127.0.0.1:5500/Fashion_store/frontend/customer/home.html
start http://localhost:5232/Admin/Account/Login

echo.
echo ========================================================
echo   HE THONG DA CHAY THANH CONG!
echo   - User Home (Load DB):     http://127.0.0.1:5500/Fashion_store/frontend/customer/home.html
echo   - Admin Quan Ly Tai Khoan: http://localhost:5232/Admin/Account/AccountManagement
echo   - Admin Quan Ly San Pham:  http://localhost:5232/Admin/Product/ProductManagement
echo ========================================================
pause

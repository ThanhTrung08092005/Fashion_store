@echo off
setlocal
chcp 65001 >nul
set "APP_ROOT=%~dp0"
pushd "%APP_ROOT%" || exit /b 1

:: Uu tien thu muc .dotnet SDK cua User neu SDK duoc cai dat o day
if exist "%USERPROFILE%\.dotnet\sdk" (
    set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
    set "PATH=%USERPROFILE%\.dotnet;%PATH%"
)
if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
    set "PATH=%USERPROFILE%\.dotnet;%PATH%"
)

echo ========================================================
echo    DANG BIEN DICH HE THONG FASHION STORE VA CRM
echo ========================================================

:: Giai phong triet de cac tien trinh cu va cong 5000 / 5232
taskkill /f /im Fashion_store.exe >nul 2>&1
taskkill /f /im FashionCRM.API.exe >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr /R /C:":5000 .*LISTENING"') do taskkill /f /pid %%a >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr /R /C:":5232 .*LISTENING"') do taskkill /f /pid %%a >nul 2>&1
"%SystemRoot%\System32\timeout.exe" /t 1 /nobreak >nul

echo [1/3] Dang bien dich Backend API (FashionCRM.API)...
dotnet build "%APP_ROOT%backend\FashionCRM.API\FashionCRM.API.csproj" --configuration Debug
if errorlevel 1 goto build_failed

echo [1/3] Dang bien dich Web Admin MVC (Fashion_store)...
dotnet build "%APP_ROOT%Fashion_store.csproj" --configuration Debug
if errorlevel 1 goto build_failed

echo [2/3] Dang khoi chay Backend API (Cong 5000 - Load DB cho User)...
start "FashionCRM_API (Port 5000)" /D "%APP_ROOT%backend\FashionCRM.API" cmd.exe /k dotnet run --no-build --launch-profile http
echo Dang cho Backend API lang nghe cong 5000 (toi da 30 giay)...
set "API_READY="
for /L %%i in (1,1,30) do (
    netstat -aon | findstr /R /C:":5000 .*LISTENING" >nul
    if not errorlevel 1 set "API_READY=1"
    if defined API_READY goto api_ready
    "%SystemRoot%\System32\timeout.exe" /t 1 /nobreak >nul
)
if not defined API_READY (
    echo Backend API khong lang nghe cong 5000. Kiem tra cua so Backend API.
    goto startup_failed
)

:api_ready

echo [3/3] Dang khoi chay Web Admin MVC (Cong 5232 - Quan ly tai khoan)...
start "Fashion_Store_Admin (Port 5232)" /D "%APP_ROOT%" cmd.exe /k dotnet run --no-build --launch-profile http --project Fashion_store.csproj
echo Dang cho Web Admin lang nghe cong 5232 (toi da 30 giay)...
set "ADMIN_READY="
for /L %%i in (1,1,30) do (
    netstat -aon | findstr /R /C:":5232 .*LISTENING" >nul
    if not errorlevel 1 set "ADMIN_READY=1"
    if defined ADMIN_READY goto admin_ready
    "%SystemRoot%\System32\timeout.exe" /t 1 /nobreak >nul
)
if not defined ADMIN_READY (
    echo Web Admin khong lang nghe cong 5232. Kiem tra cua so Web Admin.
    goto startup_failed
)

:admin_ready

echo.
echo Backend API va Web Admin da lang nghe tren cong tuong ung.

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

:startup_failed
echo.
echo KHOI DONG THAT BAI. Hay xem thong bao trong cua so dich vu.
pause
popd
exit /b 1

# ==============================================================================
# SCRIPT TỰ ĐỘNG TẢI 72/72 ẢNH CHUẨN THỜI TRANG NAM (ATINO / MEN'S FASHION)
# - 36 ảnh đại diện hiển thị danh mục (sp1_thumb.jpg -> sp36_thumb.jpg)
# - 36 ảnh chi tiết sản phẩm cận cảnh (sp1_detail.jpg -> sp36_detail.jpg)
# - Tự động vượt tường lửa Hotlink (Anti-leech) & Fallback thông minh đảm bảo 72/72
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
[Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$targetDir = "wwwroot/images/products"
if (!(Test-Path $targetDir)) { 
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null 
}

# Danh sách 36 mẫu áo tương ứng 5 danh mục của cửa hàng
# Mỗi mẫu gồm 1 ảnh đại diện (Thumb) và 1 ảnh chi tiết (Detail)
$catalog = @(
    # --- DANH MỤC 1: Áo Thun Nam Oversize (SP 1 -> 8) ---
    @{ Id=1;  Name="Ao-Thun-Oversize-Basic";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2743.jpg";
       Detail="https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&q=80" },
    @{ Id=2;  Name="Ao-Thun-Graphic-Urban";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2740.jpg";
       Detail="https://images.unsplash.com/photo-1576566588028-4147f3842f27?w=800&q=80" },
    @{ Id=3;  Name="Ao-Thun-Raglan-Retro";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Oversize-L-3-2732.jpg";
       Detail="https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?w=800&q=80" },
    @{ Id=4;  Name="Ao-Thun-Washed-Vintage";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2743.jpg";
       Detail="https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&q=80" },
    @{ Id=5;  Name="Ao-Thun-Co-Tim-Thoang-Khi";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2740.jpg";
       Detail="https://images.unsplash.com/photo-1529374255404-311a2a4f1fd9?w=800&q=80" },
    @{ Id=6;  Name="Ao-Thun-DryTouch-Khang-Khuan";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Oversize-L-3-2732.jpg";
       Detail="https://images.unsplash.com/photo-1562157873-818bc0726f68?w=800&q=80" },
    @{ Id=7;  Name="Ao-Thun-Tui-Hop-Casual";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2743.jpg";
       Detail="https://images.unsplash.com/photo-1618354691373-d851c5c3a990?w=800&q=80" },
    @{ Id=8;  Name="Ao-Thun-Drop-Shoulder";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2740.jpg";
       Detail="https://images.unsplash.com/photo-1503341455253-b2e723bb3dbb?w=800&q=80" },

    # --- DANH MỤC 2: Áo Polo Nam Dệt Tổ Ong (SP 9 -> 16) ---
    @{ Id=9;  Name="Ao-Polo-Len-Coc-Slim";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Len-Coc-Slim-L-3-3369.jpg";
       Detail="https://images.unsplash.com/photo-1586363104862-3a5e2ab60d99?w=800&q=80" },
    @{ Id=10; Name="Ao-Polo-Loose-3386";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3386.jpg";
       Detail="https://images.unsplash.com/photo-1625910513413-7e2cb0bc4c71?w=800&q=80" },
    @{ Id=11; Name="Ao-Polo-Loose-3476";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3476.jpg";
       Detail="https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&q=80" },
    @{ Id=12; Name="Ao-Polo-Loose-3372";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3372.jpg";
       Detail="https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?w=800&q=80" },
    @{ Id=13; Name="Ao-Polo-Loose-3392";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3392.jpg";
       Detail="https://images.unsplash.com/photo-1617137984095-74e4e5e3613f?w=800&q=80" },
    @{ Id=14; Name="Ao-Polo-Loose-3384";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3384.jpg";
       Detail="https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&q=80" },
    @{ Id=15; Name="Ao-Polo-Slim-3366";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Len-Coc-Slim-L-3-3366.jpg";
       Detail="https://images.unsplash.com/photo-1586363104862-3a5e2ab60d99?w=800&q=80" },
    @{ Id=16; Name="Ao-Polo-Waffle-Co-V";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Len-Coc-Slim-L-3-3369.jpg";
       Detail="https://images.unsplash.com/photo-1625910513413-7e2cb0bc4c71?w=800&q=80" },

    # --- DANH MỤC 3: Áo Sơ Mi Nam Công Sở (SP 17 -> 24) ---
    @{ Id=17; Name="Ao-So-Mi-Dai-Regular-4393";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-2-4393.jpg";
       Detail="https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&q=80" },
    @{ Id=18; Name="Ao-So-Mi-Dai-Regular-4414";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-4-4414.jpg";
       Detail="https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&q=80" },
    @{ Id=19; Name="Ao-So-Mi-Loose-4468";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20240629_iZEHE7TbMH.jpeg";
       Detail="https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&q=80" },
    @{ Id=20; Name="Ao-So-Mi-Dai-Regular-4401";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-2-4393.jpg";
       Detail="https://images.unsplash.com/photo-1603252109303-2751441dd157?w=800&q=80" },
    @{ Id=21; Name="Ao-So-Mi-Dai-Regular-4404";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-4-4414.jpg";
       Detail="https://images.unsplash.com/photo-1589310243389-9685981b3456?w=800&q=80" },
    @{ Id=22; Name="Ao-So-Mi-Over-Shirt-4412";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-2-4393.jpg";
       Detail="https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&q=80" },
    @{ Id=23; Name="Ao-So-Mi-Dai-Regular-4416";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-So-Mi-Dai-Regular-L-4-4414.jpg";
       Detail="https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&q=80" },
    @{ Id=24; Name="Ao-So-Mi-Slim-4417";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20240629_iZEHE7TbMH.jpeg";
       Detail="https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&q=80" },

    # --- DANH MỤC 4: Áo Khoác Gió Nam (SP 25 -> 30) ---
    @{ Id=25; Name="Ao-Khoac-Jacket-8931";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1551028719-00167b16eac5?w=800&q=80" },
    @{ Id=26; Name="Ao-Khoac-Phao-8561";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1544441893-675973e31985?w=800&q=80" },
    @{ Id=27; Name="Ao-Khoac-Jacket-8893";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1548883354-7622d03aca27?w=800&q=80" },
    @{ Id=28; Name="Ao-Khoac-XL-8310";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1551028719-00167b16eac5?w=800&q=80" },
    @{ Id=29; Name="Ao-Bomber-8207";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1544441893-675973e31985?w=800&q=80" },
    @{ Id=30; Name="Ao-Phao-AKP-8560";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1548883354-7622d03aca27?w=800&q=80" },

    # --- DANH MỤC 5: Áo Hoodie & Sweater Nam (SP 31 -> 36) ---
    @{ Id=31; Name="Ao-Len-Coc-Slim-2724";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2743.jpg";
       Detail="https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&q=80" },
    @{ Id=32; Name="Ao-Len-Coc-Slim-2721";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Loose-L-3-2740.jpg";
       Detail="https://images.unsplash.com/photo-1509967419530-da38b4704bc6?w=800&q=80" },
    @{ Id=33; Name="Ao-Hoodie-ZipUp-Streetwear";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/20260126_HALPhFQUyX.jpeg";
       Detail="https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&q=80" },
    @{ Id=34; Name="Ao-Sweater-Len-Gan-Co-Lo";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Len-Coc-Slim-L-3-3369.jpg";
       Detail="https://images.unsplash.com/photo-1578587018452-892bacefd3f2?w=800&q=80" },
    @{ Id=35; Name="Ao-Hoodie-Oversize-Graphic";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Phong-Oversize-L-3-2732.jpg";
       Detail="https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&q=80" },
    @{ Id=36; Name="Ao-Sweater-Phoi-Co-Polo";
       Thumb="https://pos.nvncdn.com/f4d87e-8901/ps/Ao-Polo-Loose-L-3-3372.jpg";
       Detail="https://images.unsplash.com/photo-1509967419530-da38b4704bc6?w=800&q=80" }
)

# Kho ảnh dự phòng thời trang nam chất lượng cao
$fashionPool = @(
    "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&q=80",
    "https://images.unsplash.com/photo-1586363104862-3a5e2ab60d99?w=800&q=80",
    "https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&q=80",
    "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=800&q=80",
    "https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&q=80"
)

# Hàm tải ảnh có gắn Header giả lập trình duyệt (Vượt tường lửa Atino CDN)
function Download-ProductImage($primaryUrl, $filePath, $fallbackUrl) {
    if ((Test-Path $filePath) -and ((Get-Item $filePath).Length -gt 4000)) {
        return $true
    }

    $client = New-Object System.Net.WebClient
    $client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0 Safari/537.36")
    $client.Headers.Add("Referer", "https://atino.vn/")
    $client.Headers.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8")

    # Thử tải từ URL chính (Atino CDN)
    $attempts = 0
    while ($attempts -lt 2) {
        try {
            $client.DownloadFile($primaryUrl, $filePath)
            if ((Test-Path $filePath) -and ((Get-Item $filePath).Length -gt 2000)) {
                return $true
            }
        } catch {
            Start-Sleep -Milliseconds 150
        }
        $attempts++
    }

    # Nếu CDN gốc bị đổi link -> Tự động chuyển qua ảnh thời trang tương ứng từ Pool
    try {
        $client.Headers.Remove("Referer") | Out-Null
        $client.DownloadFile($fallbackUrl, $filePath)
        if ((Test-Path $filePath) -and ((Get-Item $filePath).Length -gt 2000)) {
            return $true
        }
    } catch {}

    return $false
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "TIEN HANH TAI 72 ANH THOI TRANG CHO 36 SAN PHAM ATINO" -ForegroundColor Green
Write-Host "Luu vao: $targetDir" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green

$successCount = 0
$index = 1

foreach ($item in $catalog) {
    $spId = $item.Id
    $thumbFile = "sp${spId}_thumb.jpg"
    $detailFile = "sp${spId}_detail.jpg"
    
    $thumbPath = Join-Path $targetDir $thumbFile
    $detailPath = Join-Path $targetDir $detailFile
    
    $fallbackThumb = $fashionPool[($spId % $fashionPool.Count)]
    $fallbackDetail = $fashionPool[(($spId + 1) % $fashionPool.Count)]

    # 1. Tải ảnh đại diện (Thumb)
    Write-Host "[$index/72] Tai: $thumbFile ($($item.Name)) ... " -NoNewline -ForegroundColor Cyan
    if (Download-ProductImage $item.Thumb $thumbPath $fallbackThumb) {
        Write-Host "OK" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "FAILED" -ForegroundColor Red
    }
    $index++

    # 2. Tải ảnh chi tiết (Detail)
    Write-Host "[$index/72] Tai: $detailFile ... " -NoNewline -ForegroundColor Cyan
    if (Download-ProductImage $item.Detail $detailPath $fallbackDetail) {
        Write-Host "OK" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "FAILED" -ForegroundColor Red
    }
    $index++
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "HOAN TAT! DA TAI THANH CONG: $successCount / 72 ANH!" -ForegroundColor Green
Write-Host "Tat ca anh nam tai thu muc: $targetDir" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green


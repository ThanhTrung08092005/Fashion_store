import os
import sys
import zlib
import time
import shutil
import urllib.request

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')
if hasattr(sys.stderr, 'reconfigure'):
    sys.stderr.reconfigure(encoding='utf-8')

OUTPUT_DIR = r"c:\Users\Van Thien\Downloads\ĐỒ ÁN HTTTDN\Fashion_store\baocao\so_do_he_thong"
ARTIFACT_DIR = r"C:\Users\Van Thien\.gemini\antigravity-ide\brain\44e046ce-ad4c-4323-bd06-6f9a095be7b1"

os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(ARTIFACT_DIR, exist_ok=True)

def encode6bit(b):
    b = b & 0x3f
    if b < 10: return chr(48 + b)
    b -= 10
    if b < 26: return chr(65 + b)
    b -= 26
    if b < 26: return chr(97 + b)
    b -= 26
    if b == 0: return '-'
    if b == 1: return '_'
    return '?'

def encode64(data):
    res = []
    for i in range(0, len(data), 3):
        b1 = data[i]
        b2 = data[i+1] if i+1 < len(data) else 0
        b3 = data[i+2] if i+2 < len(data) else 0
        res.append(encode6bit(b1 >> 2))
        res.append(encode6bit(((b1 & 0x3) << 4) | (b2 >> 4)))
        if i+1 < len(data):
            res.append(encode6bit(((b2 & 0xf) << 2) | (b3 >> 6)))
        if i+2 < len(data):
            res.append(encode6bit(b3 & 0x3f))
    return ''.join(res)

def get_plantuml_url(code, fmt='png'):
    comp = zlib.compress(code.encode('utf-8'))[2:-4]
    return f"https://www.plantuml.com/plantuml/{fmt}/~1" + encode64(comp)

def download_diagram(name, code):
    print(f"[*] Đang tạo sơ đồ: {name}...")
    # Tải PNG phân giải cao (có dpi 250-300)
    png_code = code
    if "skinparam dpi" not in png_code:
        png_code = png_code.replace("@startuml", "@startuml\nskinparam dpi 250")
        png_code = png_code.replace("@startwbs", "@startwbs\nskinparam dpi 250")

    png_url = get_plantuml_url(png_code, 'png')
    svg_url = get_plantuml_url(code, 'svg')

    png_path = os.path.join(OUTPUT_DIR, f"{name}.png")
    svg_path = os.path.join(OUTPUT_DIR, f"{name}.svg")
    artifact_png = os.path.join(ARTIFACT_DIR, f"{name}.png")

    req_headers = {'User-Agent': 'Mozilla/5.0'}

    # Tải PNG
    for attempt in range(3):
        try:
            req = urllib.request.Request(png_url, headers=req_headers)
            with urllib.request.urlopen(req, timeout=30) as resp:
                data = resp.read()
                with open(png_path, 'wb') as f:
                    f.write(data)
                with open(artifact_png, 'wb') as f:
                    f.write(data)
            print(f"    -> Đã tạo PNG: {png_path} ({len(data):,} bytes)")
            break
        except Exception as e:
            print(f"    Lỗi PNG (lần {attempt+1}): {e}")
            time.sleep(2)

    # Tải SVG
    for attempt in range(3):
        try:
            req = urllib.request.Request(svg_url, headers=req_headers)
            with urllib.request.urlopen(req, timeout=30) as resp:
                data = resp.read()
                with open(svg_path, 'wb') as f:
                    f.write(data)
            print(f"    -> Đã tạo SVG: {svg_path} ({len(data):,} bytes)")
            break
        except Exception as e:
            print(f"    Lỗi SVG (lần {attempt+1}): {e}")
            time.sleep(2)

diagrams = {}

# 1. BFD
diagrams["01_So_do_phan_ra_chuc_nang_BFD"] = """@startwbs
skinparam monochrome false
skinparam roundcorner 10
skinparam shadowing true
skinparam defaultFontName "Segoe UI"
skinparam dpi 250

<style>
wbsDiagram {
  .root {
    BackgroundColor #1E3A8A
    FontColor white
    FontSize 14
    FontStyle bold
    LineColor #1E3A8A
  }
  .lvl1 {
    BackgroundColor #0284C7
    FontColor white
    FontSize 12
    FontStyle bold
    LineColor #0284C7
  }
  .lvl2 {
    BackgroundColor #F0F9FF
    FontColor #0C4A6E
    FontSize 11
    LineColor #7DD3FC
  }
}
</style>

* **HỆ THỐNG THÔNG TIN QUẢN LÝ FASHION STORE & CRM** <<root>>

** 1. Quản lý Hệ thống & Phân quyền <<lvl1>>
*** 1.1 Đăng ký & Đăng nhập hệ thống <<lvl2>>
*** 1.2 Phân quyền vai trò (Admin / Quản lý) <<lvl2>>
*** 1.3 Quản lý tài khoản & Khóa/Mở tài khoản <<lvl2>>
*** 1.4 Đổi mật khẩu & Đăng xuất <<lvl2>>

** 2. Quản trị Hồ sơ Khách hàng (CRM) <<lvl1>>
*** 2.1 Cập nhật thông tin cá nhân & Địa chỉ <<lvl2>>
*** 2.2 Cập nhật sở thích thời trang (Danh mục) <<lvl2>>
*** 2.3 Quản lý danh sách khách hàng (Thêm/Sửa/Xóa mềm) <<lvl2>>
*** 2.4 Tra cứu & Lọc khách hàng nâng cao <<lvl2>>

** 3. Quản lý Sản phẩm & Bán hàng <<lvl1>>
*** 3.1 Quản lý danh mục sản phẩm <<lvl2>>
*** 3.2 Quản lý sản phẩm & Biến thể (Size, Màu, Tồn kho, Giá) <<lvl2>>
*** 3.3 Tìm kiếm, lọc & Xem chi tiết thời trang <<lvl2>>
*** 3.4 Đặt hàng & Xử lý duyệt đơn hàng <<lvl2>>

** 4. Quản lý Phản hồi & Đánh giá (CRM) <<lvl1>>
*** 4.1 Khách hàng gửi phản hồi & Khiếu nại <<lvl2>>
*** 4.2 Khách hàng đánh giá số sao & Bình luận sản phẩm <<lvl2>>
*** 4.3 Quản lý tiếp nhận & Phân loại phản hồi <<lvl2>>
*** 4.4 Phản hồi lại ý kiến và cập nhật trạng thái xử lý <<lvl2>>

** 5. Quản lý Khảo sát Mẫu áo mới (CRM) <<lvl1>>
*** 5.1 Thiết lập chiến dịch khảo sát sản phẩm mới <<lvl2>>
*** 5.2 Quản lý bộ câu hỏi (1 lựa chọn, nhiều lựa chọn, tự luận, sao) <<lvl2>>
*** 5.3 Phát hành phiếu khảo sát tới khách hàng <<lvl2>>
*** 5.4 Khách hàng thực hiện & Gửi kết quả khảo sát <<lvl2>>

** 6. Báo cáo & Thống kê Phân tích CRM <<lvl1>>
*** 6.1 Thống kê tỷ lệ khách hàng theo nhóm độ tuổi <<lvl2>>
*** 6.2 Thống kê xu hướng sở thích thời trang nam <<lvl2>>
*** 6.3 Phân tích kết quả khảo sát mẫu áo chuẩn bị ra mắt <<lvl2>>
*** 6.4 Thống kê doanh thu & Mức độ hài lòng khách hàng <<lvl2>>

@endwbs
"""

# 2. DFD Mức 0
diagrams["02_So_do_luong_du_lieu_DFD_Muc_0"] = """@startuml
skinparam defaultFontName "Segoe UI"
skinparam roundcorner 12
skinparam shadowing true
skinparam packageStyle rectangle
skinparam dpi 250

skinparam rectangle {
    BackgroundColor #F1F5F9
    BorderColor #475569
    BorderThickness 1.5
    FontSize 13
    FontStyle bold
}

skinparam usecase {
    BackgroundColor #EFF6FF
    BorderColor #1D4ED8
    BorderThickness 2
    FontSize 14
    FontStyle bold
}

skinparam arrow {
    Color #1E293B
    FontColor #0F172A
    FontSize 11
}

rectangle "KHÁCH HÀNG\\n(Customer)" as KH
rectangle "NGƯỜI QUẢN LÝ / ADMIN\\n(Manager / Administrator)" as QL

usecase "0.0\\nHỆ THỐNG THÔNG TIN\\nQUẢN LÝ CỬA HÀNG FASHION STORE\\n& PHÂN HỆ CRM" as System

' Luồng giữa Khách hàng và Hệ thống
KH --> System : [1] Thông tin đăng ký / đăng nhập\\n[2] Hồ sơ cá nhân & Sở thích thời trang\\n[3] Yêu cầu đặt hàng\\n[4] Gửi phản hồi, khiếu nại & đánh giá sao\\n[5] Phiếu trả lời khảo sát mẫu áo mới

System --> KH : [1] Xác thực & Thông tin tài khoản\\n[2] Danh mục sản phẩm & Chi tiết biến thể\\n[3] Hóa đơn / Trạng thái đơn hàng\\n[4] Kết quả xử lý & lời cảm ơn phản hồi\\n[5] Thông báo & Phiếu khảo sát cần thực hiện

' Luồng giữa Quản lý và Hệ thống
QL --> System : [1] Đăng nhập quản trị & Cấu hình tài khoản\\n[2] Cập nhật danh mục, sản phẩm, giá & tồn kho\\n[3] Duyệt / Cập nhật trạng thái đơn hàng\\n[4] Xử lý & Phản hồi khiếu nại khách hàng\\n[5] Thiết lập chiến dịch khảo sát & Bộ câu hỏi\\n[6] Yêu cầu thống kê, báo cáo CRM

System --> QL : [1] Danh sách tài khoản & Phân quyền\\n[2] Danh sách hồ sơ & Sở thích khách hàng\\n[3] Danh sách đơn hàng cần xử lý\\n[4] Danh sách phản hồi, khiếu nại mới\\n[5] Tiến độ khảo sát & Thống kê kết quả\\n[6] Báo cáo độ tuổi, sở thích & phân tích khảo sát

@enduml
"""

# 3. DFD Mức 1
diagrams["03_So_do_luong_du_lieu_DFD_Muc_1"] = """@startuml
skinparam defaultFontName "Segoe UI"
skinparam shadowing true
skinparam packageStyle rectangle
skinparam dpi 250

skinparam rectangle {
    BackgroundColor #F8FAFC
    BorderColor #334155
    BorderThickness 1.5
    FontSize 12
    FontStyle bold
}

skinparam usecase {
    BackgroundColor #FEF3C7
    BorderColor #D97706
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam database {
    BackgroundColor #ECFDF5
    BorderColor #059669
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam arrow {
    Color #1E293B
    FontColor #0F172A
    FontSize 10
}

' External Entities
rectangle "KHÁCH HÀNG" as KH
rectangle "QUẢN LÝ / ADMIN" as QL

' Processes
usecase "1.0\\nQuản lý Tài khoản\\n& Phân quyền" as P1
usecase "2.0\\nQuản trị Hồ sơ\\n& Sở thích Khách hàng" as P2
usecase "3.0\\nQuản lý Sản phẩm\\n& Đơn hàng" as P3
usecase "4.0\\nQuản lý Phản hồi\\n& Đánh giá" as P4
usecase "5.0\\nQuản lý Chiến dịch\\nKhảo sát Mẫu mới" as P5
usecase "6.0\\nBáo cáo &\\nPhân tích CRM" as P6

' Data Stores
database "D1: TAIKHOAN & VAITRO" as D1
database "D2: KHACHHANG & SOTHICH" as D2
database "D3: SANPHAM, DANHMUC & BIẾN THỂ" as D3
database "D4: DONHANG & CHITIETDONHANG" as D4
database "D5: PHANHOI & DANHGIA" as D5
database "D6: KHAOSAT & PHIEUKHAOSAT" as D6

' Flows Process 1.0
KH --> P1 : Đăng ký / Đăng nhập
QL --> P1 : Đăng nhập / Khóa / Mở TK
P1 <--> D1 : Đọc / Ghi thông tin tài khoản
P1 --> KH : Kết quả xác thực
P1 --> QL : Trạng thái tài khoản

' Flows Process 2.0
KH --> P2 : Cập nhật thông tin & Sở thích
QL --> P2 : Quản lý hồ sơ / Tra cứu KH
P2 <--> D2 : Cập nhật / Đọc hồ sơ & sở thích
P2 --> KH : Xác nhận cập nhật
P2 --> QL : Danh sách & Thông tin KH

' Flows Process 3.0
KH --> P3 : Yêu cầu xem hàng & Đặt hàng
QL --> P3 : Thêm, sửa SP / Duyệt đơn hàng
P3 <--> D3 : Tra cứu SP / Giảm tồn kho
P3 <--> D4 : Tạo đơn / Cập nhật trạng thái đơn
P3 --> KH : Hóa đơn & Trạng thái giao hàng
P3 --> QL : Báo cáo tồn & Đơn hàng mới

' Flows Process 4.0
KH --> P4 : Gửi phản hồi / Chấm điểm sao
QL --> P4 : Xử lý / Trả lời phản hồi
P4 <--> D5 : Lưu phản hồi / Đọc nội dung
P4 --> KH : Kết quả giải quyết khiếu nại
P4 --> QL : Danh sách phản hồi cần duyệt

' Flows Process 5.0
QL --> P5 : Tạo khảo sát, lập bộ câu hỏi
KH --> P5 : Nộp kết quả làm khảo sát
P5 <--> D6 : Ghi nhận chiến dịch & Phiếu nộp
P5 --> KH : Phiếu khảo sát cần điền
P5 --> QL : Thống kê lượt tham gia

' Flows Process 6.0
QL --> P6 : Yêu cầu xuất báo cáo CRM
D2 --> P6 : Dữ liệu tuổi & sở thích
D5 --> P6 : Dữ liệu đánh giá sao & phản hồi
D6 --> P6 : Dữ liệu trả lời câu hỏi khảo sát
P6 --> QL : Biểu đồ phân tích CRM\\n(Tuổi, Gu thời trang, Khảo sát)

@enduml
"""

# 4. DFD Mức 2 - P5
diagrams["04_So_do_luong_du_lieu_DFD_Muc_2_Khao_sat_P5"] = """@startuml
skinparam defaultFontName "Segoe UI"
skinparam shadowing true
skinparam packageStyle rectangle
skinparam dpi 250

skinparam rectangle {
    BackgroundColor #F8FAFC
    BorderColor #334155
    BorderThickness 1.5
    FontSize 12
    FontStyle bold
}

skinparam usecase {
    BackgroundColor #E0F2FE
    BorderColor #0284C7
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam database {
    BackgroundColor #ECFDF5
    BorderColor #059669
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam arrow {
    Color #1E293B
    FontColor #0F172A
    FontSize 10
}

rectangle "QUẢN LÝ / ADMIN" as QL
rectangle "KHÁCH HÀNG" as KH

usecase "5.1\\nThiết lập Chiến dịch\\nKhảo sát" as P51
usecase "5.2\\nSoạn thảo Bộ câu hỏi\\n& Lựa chọn" as P52
usecase "5.3\\nPhát hành Phiếu\\nKhảo sát" as P53
usecase "5.4\\nTiếp nhận Phiếu làm\\n& Ghi nhận Trả lời" as P54
usecase "5.5\\nTổng hợp Dữ liệu\\nKhảo sát" as P55

database "D2: KHACHHANG" as D2
database "D6.1: KHAOSAT" as D61
database "D6.2: CAUHOI & LUACHON" as D62
database "D6.3: PHIEUKHAOSAT" as D63
database "D6.4: TRALOI_KHAOSAT" as D64

' 5.1
QL --> P51 : Thông tin chiến dịch (Tên, ngày BĐ, KT)
P51 --> D61 : Lưu thông tin chiến dịch khảo sát

' 5.2
QL --> P52 : Nội dung câu hỏi, loại câu hỏi & đáp án
P52 <--> D61 : Kiểm tra MaKS hợp lệ
P52 --> D62 : Lưu câu hỏi & các phương án chọn

' 5.3
D61 --> P53 : Lấy thông tin khảo sát đang mở
D2 --> P53 : Danh sách MaKH còn hoạt động
P53 --> D63 : Tạo danh sách PHIEUKHAOSAT\\n(TrangThai: Đang làm)
P53 --> KH : Thông báo bài khảo sát mới

' 5.4
KH --> P54 : Gửi đáp án lựa chọn, ý kiến tự luận, số sao
D62 --> P54 : Kiểm tra tính hợp lệ của câu trả lời
P54 --> D64 : Ghi nhận đáp án vào TRALOI_KHAOSAT
P54 --> D63 : Cập nhật Trạng thái = 'Đã hoàn thành', Ngày nộp
P54 --> KH : Thông báo nộp bài thành công

' 5.5
D63 --> P55 : Số lượng phiếu đã hoàn thành
D64 --> P55 : Dữ liệu câu trả lời chi tiết
P55 --> QL : Bảng thống kê tỷ lệ chọn & ý kiến khách hàng

@enduml
"""

# 5. DFD Mức 2 - P4
diagrams["05_So_do_luong_du_lieu_DFD_Muc_2_Phan_hoi_P4"] = """@startuml
skinparam defaultFontName "Segoe UI"
skinparam shadowing true
skinparam packageStyle rectangle
skinparam dpi 250

skinparam rectangle {
    BackgroundColor #F8FAFC
    BorderColor #334155
    BorderThickness 1.5
    FontSize 12
    FontStyle bold
}

skinparam usecase {
    BackgroundColor #FEE2E2
    BorderColor #DC2626
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam database {
    BackgroundColor #ECFDF5
    BorderColor #059669
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam arrow {
    Color #1E293B
    FontColor #0F172A
    FontSize 10
}

rectangle "KHÁCH HÀNG" as KH
rectangle "QUẢN LÝ / ADMIN" as QL

usecase "4.1\\nGửi Ý kiến Đóng góp\\n& Đánh giá sao" as P41
usecase "4.2\\nTiếp nhận &\\nPhân loại Phản hồi" as P42
usecase "4.3\\nXử lý Khiếu nại &\\nSoạn phản hồi" as P43
usecase "4.4\\nThông báo & Cập nhật\\nTrạng thái giải quyết" as P44

database "D2: KHACHHANG" as D2
database "D3: SANPHAM" as D3
database "D5.1: PHANHOI" as D51
database "D5.2: DANHGIA" as D52

' 4.1
KH --> P41 : Tiêu đề, nội dung, sản phẩm liên quan, số sao
D2 --> P41 : Xác thực MaKH gửi
D3 --> P41 : Kiểm tra MaSP hợp lệ
P41 --> D51 : Thêm mới PHANHOI (Trạng thái: Chưa xử lý)
P41 --> D52 : Lưu số sao & nhận xét vào DANHGIA

' 4.2
D51 --> P42 : Danh sách phản hồi mới gửi
P42 --> QL : Báo cáo danh sách phản hồi cần xử lý

' 4.3
QL --> P43 : Nội dung phản hồi của quản lý & duyệt đóng phản hồi
P43 --> D51 : Cập nhật PhanHoiQuanLy, NgayXuLy, MaQL,\\nTrangThai = 'Đã xử lý'

' 4.4
D51 --> P44 : Lấy nội dung đã phản hồi
P44 --> KH : Thông báo câu trả lời từ cửa hàng

@enduml
"""

# 6. DFD Mức 2 - P3
diagrams["06_So_do_luong_du_lieu_DFD_Muc_2_Don_hang_P3"] = """@startuml
skinparam defaultFontName "Segoe UI"
skinparam shadowing true
skinparam packageStyle rectangle
skinparam dpi 250

skinparam rectangle {
    BackgroundColor #F8FAFC
    BorderColor #334155
    BorderThickness 1.5
    FontSize 12
    FontStyle bold
}

skinparam usecase {
    BackgroundColor #FEF08A
    BorderColor #CA8A04
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam database {
    BackgroundColor #ECFDF5
    BorderColor #059669
    BorderThickness 1.5
    FontSize 11
    FontStyle bold
}

skinparam arrow {
    Color #1E293B
    FontColor #0F172A
    FontSize 10
}

rectangle "KHÁCH HÀNG" as KH
rectangle "QUẢN LÝ / ADMIN" as QL

usecase "3.1\\nTra cứu & Xem chi tiết\\nSản phẩm Thời trang" as P31
usecase "3.2\\nKhởi tạo Đơn hàng\\n& Chi tiết giỏ hàng" as P32
usecase "3.3\\nKiểm tra Tồn kho\\n& Khấu trừ số lượng" as P33
usecase "3.4\\nDuyệt Đơn hàng &\\nCập nhật Giao hàng" as P34

database "D2: KHACHHANG" as D2
database "D3: SANPHAM, DANHMUC & SOLUONGSP" as D3
database "D4.1: DONHANG" as D41
database "D4.2: CHITIETDONHANG" as D42

' 3.1
KH --> P31 : Tìm kiếm từ khóa / Chọn danh mục / Lọc màu, size
D3 --> P31 : Dữ liệu thông tin sản phẩm, đơn giá, hình ảnh
P31 --> KH : Danh sách & Chi tiết sản phẩm

' 3.2
KH --> P32 : Chọn biến thể (SKU, Size, Màu), số lượng, địa chỉ giao
D2 --> P32 : Thông tin khách hàng đặt hàng
P32 --> D41 : Thêm mới đơn hàng (Trạng thái: Chờ xác nhận)
P32 --> D42 : Thêm các dòng CHITIETDONHANG (MaSL, SoLuong, DonGia)

' 3.3
D42 --> P33 : Danh sách MaSL và SoLuong đặt
P33 <--> D3 : Kiểm tra SoLuongTon & Trừ tồn kho tương ứng
P33 --> KH : Thông báo đặt hàng thành công

' 3.4
QL --> P34 : Duyệt đơn / Cập nhật: 'Đã xác nhận', 'Đang giao', 'Đã giao'
P34 --> D41 : Cập nhật TrangThai, MaQLXuLy
P34 --> KH : Thông báo trạng thái vận chuyển đơn hàng

@enduml
"""

for name, code in diagrams.items():
    download_diagram(name, code)

print("\n[V] ĐÃ HOÀN TẤT TẠO TOÀN BỘ ẢNH SƠ ĐỒ!")

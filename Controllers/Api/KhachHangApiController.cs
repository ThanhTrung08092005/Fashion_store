using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fashion_store.Data;

namespace Fashion_store.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhachHangApiController : ControllerBase
    {
        private readonly FashionStoreDbContext _context;

        public KhachHangApiController(FashionStoreDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/KhachHangApi - Quản lý xem danh sách khách hàng
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.KhachHang
                .AsNoTracking()
                .Where(k => !k.DaXoa)
                .Select(k => new
                {
                    k.MaKH,
                    k.HoTen,
                    k.Email,
                    k.SoDienThoai,
                    k.GioiTinh,
                    k.NgaySinh,
                    k.DiaChi,
                    k.NgayDangKy,
                    TrangThaiTaiKhoan = k.TaiKhoan != null && k.TaiKhoan.TrangThai ? "Hoạt động" : "Đã khóa"
                }).ToListAsync();

            return Ok(list);
        }

        // 2. PUT: api/KhachHangApi/5/khoa - Khóa / Mở khóa tài khoản khách hàng
        [HttpPut("{id}/khoa")]
        public async Task<IActionResult> ToggleKhoa(int id)
        {
            var khachHang = await _context.KhachHang
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKH == id);

            if (khachHang == null || khachHang.TaiKhoan == null)
                return NotFound(new { message = "Không tìm thấy khách hàng hoặc tài khoản" });

            khachHang.TaiKhoan.TrangThai = !khachHang.TaiKhoan.TrangThai;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = khachHang.TaiKhoan.TrangThai ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản",
                trangThaiMoi = khachHang.TaiKhoan.TrangThai
            });
        }

        // 3. GET: api/KhachHangApi/thong-ke-do-tuoi - Báo cáo thống kê độ tuổi khách hàng
        [HttpGet("thong-ke-do-tuoi")]
        public async Task<IActionResult> ThongKeDoTuoi()
        {
            var danhSach = await _context.KhachHang
                .AsNoTracking()
                .Where(k => !k.DaXoa && k.NgaySinh.HasValue)
                .Select(k => k.NgaySinh!.Value)
                .ToListAsync();

            var now = DateTime.Now;
            int duoi18 = 0;
            int tu18Den25 = 0;
            int tu26Den35 = 0;
            int tren35 = 0;

            foreach (var ns in danhSach)
            {
                int tuoi = now.Year - ns.Year;
                if (ns.Date > now.AddYears(-tuoi)) tuoi--;

                if (tuoi < 18) duoi18++;
                else if (tuoi <= 25) tu18Den25++;
                else if (tuoi <= 35) tu26Den35++;
                else tren35++;
            }

            return Ok(new
            {
                TongSoKhachHangCoNgaySinh = danhSach.Count,
                TiLeDoTuoi = new
                {
                    Duoi18Tuoi = duoi18,
                    Tu18Den25Tuoi = tu18Den25,
                    Tu26Den35Tuoi = tu26Den35,
                    Tren35Tuoi = tren35
                }
            });
        }
    }
}

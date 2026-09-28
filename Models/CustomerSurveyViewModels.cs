using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fashion_store.Models
{
    /// <summary>
    /// DTO tiếp nhận toàn bộ câu trả lời từ giao diện làm bài khảo sát
    /// </summary>
    public class SubmitSurveyDto
    {
        public int MaPhieuKS { get; set; }
        public List<AnswerItemDto> Answers { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết từng câu trả lời theo loại câu hỏi
    /// </summary>
    public class AnswerItemDto
    {
        public int MaCauHoi { get; set; }
        public string LoaiCauHoi { get; set; } = string.Empty;

        // Dùng cho loại 'Một lựa chọn'
        public int? MaLuaChon { get; set; }

        // Dùng cho loại 'Nhiều lựa chọn'
        public List<int>? DanhSachMaLuaChon { get; set; }

        // Dùng cho loại 'Tự luận' (lưu vào NoiDungTraLoi trong TRALOI_KHAOSAT)
        public string? NoiDungTraLoi { get; set; }

        // Dùng cho loại 'Đánh giá sao' (lưu vào SoSao: 1 -> 5 trong TRALOI_KHAOSAT)
        public int? SoSao { get; set; }
    }

    /// <summary>
    /// ViewModel truyền dữ liệu ra màn hình danh sách khảo sát của khách hàng
    /// </summary>
    public class CustomerSurveyListViewModel
    {
        // Các phiếu khảo sát cần thực hiện (TrangThai = N'Đang làm')
        public List<PhieuKhaoSat> PhieuDangLam { get; set; } = new();

        // Các phiếu đã gửi phản hồi (TrangThai = N'Đã hoàn thành')
        public List<PhieuKhaoSat> PhieuDaHoanThanh { get; set; } = new();
    }

    /// <summary>
    /// Form khách hàng gửi phản hồi. Mã khách hàng luôn được lấy từ tài khoản đăng nhập.
    /// </summary>
    public class CustomerFeedbackViewModel
    {
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string? TieuDe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi.")]
        [StringLength(4000, MinimumLength = 5, ErrorMessage = "Nội dung phải từ 5 đến 4.000 ký tự.")]
        [Display(Name = "Nội dung phản hồi")]
        public string NoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại phản hồi.")]
        [StringLength(50)]
        [Display(Name = "Loại phản hồi")]
        public string LoaiPhanHoi { get; set; } = "Chất lượng sản phẩm";

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        [Display(Name = "Đánh giá sao")]
        public int? SoSao { get; set; }

        [Display(Name = "Sản phẩm liên quan")]
        public int? MaSP { get; set; }
    }
}

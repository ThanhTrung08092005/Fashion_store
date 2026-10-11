using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_store.Models
{
    [Table("KHACHHANG_SOTHICH")]
    public class KhachHangSoThich
    {
        [Key, Column(Order = 0)]
        public int MaKH { get; set; }

        [Key, Column(Order = 1)]
        public int MaDM { get; set; }

        [ForeignKey("MaKH")]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("MaDM")]
        public virtual DanhMuc? DanhMuc { get; set; }
    }
}

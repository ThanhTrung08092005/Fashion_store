using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace Fashion_store.Services
{
    /// <summary>
    /// Phân quyền 2 tầng:
    /// - Vai trò (Admin / Quản lý / Khách hàng) quyết định cổng đăng nhập.
    /// - Chức vụ của tài khoản Quản lý quyết định chức năng được sử dụng.
    /// Admin luôn được bypass mọi kiểm tra chức vụ.
    /// </summary>
    public static class Permissions
    {
        public const string StoreManager = "Quản lý Cửa Hàng";
        public const string SalesStaff = "Nhân viên Bán Hàng";
        public const string WarehouseStaff = "Nhân viên Kho";
        public const string CrmStaff = "Nhân viên Chăm Sóc Khách Hàng";

        public static readonly string[] AllPositions =
        {
            StoreManager, SalesStaff, WarehouseStaff, CrmStaff
        };

        // Tên policy dùng chung giữa Program.cs và các attribute [Authorize].
        public const string PolicyCustomersView = "Customers.View";
        public const string PolicyCustomersManage = "Customers.Manage";
        public const string PolicyCustomersDelete = "Customers.Delete";
        public const string PolicyProductsView = "Products.View";
        public const string PolicyProductsManage = "Products.Manage";
        public const string PolicyFeedbackManage = "Feedback.Manage";

        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return user.Identity?.IsAuthenticated == true
                && (user.IsInRole("Admin") || user.HasClaim("MaVaiTro", "3"));
        }

        /// <summary>
        /// Chuẩn hoá chức vụ về 1 trong 4 giá trị chuẩn.
        /// Chấp nhận các biến thể/typo cũ như "Quản lý cửa hàng", "Quản lý của hàng", "Quản lý CRM".
        /// Giá trị lạ được xếp về Quản lý Cửa Hàng để không khoá nhầm tài khoản quản lý cũ.
        /// </summary>
        public static string Normalize(string? chucVu)
        {
            return TryNormalize(chucVu, out var position) ? position : StoreManager;
        }

        public static bool TryNormalize(string? chucVu, out string position)
        {
            var key = ToKey(chucVu);
            position = StoreManager;
            if (key.Length == 0) return false;

            // Ưu tiên "chăm sóc khách hàng" trước để không bị nhầm với "quản lý".
            if (key.Contains("cham soc") || key.Contains("cskh") || key == "crm" || key.EndsWith(" crm"))
            {
                position = CrmStaff;
                return true;
            }

            if (key.Contains("quan ly") || key.Contains("cua hang") || key.Contains("store manager")
                || key == "admin" || key == "quan tri" || key.Contains("quan tri vien"))
            {
                position = StoreManager;
                return true;
            }

            if (key.Contains("ban hang") || key.Contains("kinh doanh") || key.Contains("sales"))
            {
                position = SalesStaff;
                return true;
            }

            if (key.Contains("kho") || key.Contains("warehouse"))
            {
                position = WarehouseStaff;
                return true;
            }

            if (key.Contains("crm"))
            {
                position = CrmStaff;
                return true;
            }

            return false;
        }

        public static string PositionOf(ClaimsPrincipal user)
        {
            return Normalize(user.FindFirstValue("ChucVu"));
        }

        // ===== Khách hàng =====
        public static bool CanViewCustomers(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return true;
            var position = PositionOf(user);
            return position == StoreManager || position == SalesStaff || position == CrmStaff;
        }

        public static bool CanManageCustomers(ClaimsPrincipal user)
        {
            return CanViewCustomers(user);
        }

        public static bool CanDeleteCustomers(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return true;
            return PositionOf(user) == StoreManager;
        }

        // ===== Sản phẩm =====
        public static bool CanViewProducts(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return true;
            var position = PositionOf(user);
            return position == StoreManager || position == SalesStaff || position == WarehouseStaff;
        }

        public static bool CanManageProducts(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return true;
            var position = PositionOf(user);
            return position == StoreManager || position == WarehouseStaff;
        }

        // ===== Phản hồi / khảo sát / báo cáo =====
        public static bool CanManageFeedback(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return true;
            var position = PositionOf(user);
            return position == StoreManager || position == CrmStaff;
        }

        /// <summary>
        /// Trang chủ phù hợp với quyền hiện tại, dùng cho logo/tên thương hiệu trên navbar.
        /// </summary>
        public static string HomeUrl(ClaimsPrincipal user)
        {
            if (CanManageFeedback(user)) return "/Admin/Survey/Analytics";
            if (CanViewCustomers(user)) return "/Admin/Account/AccountManagement";
            if (CanViewProducts(user)) return "/Admin/Product/ProductManagement";
            return "/Admin/Account/Login";
        }

        private static string ToKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;
                builder.Append(char.ToLowerInvariant(character));
            }

            return string.Join(' ', builder
                .ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'd')
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}

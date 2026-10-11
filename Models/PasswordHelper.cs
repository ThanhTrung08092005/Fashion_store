namespace Fashion_store.Models;

public static class PasswordHelper
{
    public static bool VerifyAndUpgrade(TaiKhoan account, string providedPassword)
    {
        if (account.MatKhau.StartsWith("$2", StringComparison.Ordinal))
        {
            try { return BCrypt.Net.BCrypt.Verify(providedPassword, account.MatKhau); }
            catch { return false; }
        }

        if (!string.Equals(account.MatKhau, providedPassword, StringComparison.Ordinal))
            return false;

        account.MatKhau = BCrypt.Net.BCrypt.HashPassword(providedPassword);
        return true;
    }

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}

namespace HRMS.Helpers;

public static class HashHelper
{
    public static string Hash(string plainText) =>
        BCrypt.Net.BCrypt.HashPassword(plainText);

    public static bool Verify(string plainText, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainText, hash);
}

using System.Security.Cryptography;
using System.Text;

namespace TgTodo.AspNetCore.Auth;

public static class UserIdSignatureHelper
{
    public static string Sign(string userId, string signingKey)
    {
        var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(userId));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string userId, string signature, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = Sign(userId, signingKey);
        try
        {
            var expectedBytes = Convert.FromBase64String(expected);
            var actualBytes = Convert.FromBase64String(signature);
            return expectedBytes.Length == actualBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

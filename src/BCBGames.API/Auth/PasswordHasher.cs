using System.Security.Cryptography;

namespace BCBGames.API.Auth;

public static class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32;  // 256-bit
    private const int Iterations = 210_000; // reasonable default for 2025

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"PBKDF2$SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return false;
        if (!string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal)) return false;
        if (!string.Equals(parts[1], "SHA256", StringComparison.Ordinal)) return false;
        if (!int.TryParse(parts[2], out var iterations) || iterations <= 0) return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}


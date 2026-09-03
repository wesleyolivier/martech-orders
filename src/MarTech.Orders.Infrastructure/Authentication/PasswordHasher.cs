using System.Security.Cryptography;

namespace MarTech.Orders.Infrastructure.Authentication;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static byte[] CreateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

    public static bool Verify(string password, byte[] salt, byte[] expectedHash) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, salt), expectedHash);
}

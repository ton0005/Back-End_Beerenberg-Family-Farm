using FarmManagement.Application.Security;
using System.Security.Cryptography;
using System.Text;

namespace FarmManagement.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    // Geenerate Hash and salt from plain password
    // Return tuple of hash and salt
    public (string hash, string salt) Hash(string password)
    {
        // generate random salt
        byte[] saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        string salt = Convert.ToBase64String(saltBytes);

        // derive hash with PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return (hash, salt);
    }

    public bool Verify(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        string computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computedHash), Encoding.UTF8.GetBytes(hash));
    }
}
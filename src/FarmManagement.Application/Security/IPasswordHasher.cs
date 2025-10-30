namespace FarmManagement.Application.Security;

public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string salt);

}
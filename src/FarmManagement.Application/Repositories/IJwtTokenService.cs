namespace FarmManagement.Application.Repositories
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Create a signed JWT
        /// </summary>
        /// <param name="staffId">staff ID</param>
        /// <param name="email">staff email</param>
        /// <param name="expiresMinutes">token lifetime (default 60 min)</param>
        /// <returns>signed JWT token string</returns>
        string CreateToken(string staffId, string email, int expiresMinutes = 60);
    }
}
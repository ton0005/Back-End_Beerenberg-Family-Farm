using System.Threading.Tasks;

namespace FarmManagement.Application.Services
{
    public interface IIdentityService
    {
        /// <summary>
        /// Create an Identity user with given email and password, linked to a Staff record.
        /// </summary>
        /// <param name="email">The email address for the user (will also be the username)</param>
        /// <param name="password">The password for the user</param>
        /// <param name="staffId">The ID of the Staff record to link to</param>
        /// <param name="roles">Optional list of role names to assign to the user</param>
        /// <returns>Tuple containing the created user ID (if successful) and any error message.</returns>
        Task<(string? userId, string? error)> CreateUserAsync(string email, string password, int staffId, IEnumerable<string>? roles = null);

        /// <summary>
        /// Get Identity roles for a given staff member (by staffId)
        /// </summary>
        Task<string[]?> GetRolesForStaffAsync(int staffId);

        /// <summary>
        /// Replace the roles assigned to the identity user for the given staff.
        /// Ensures roles exist before assignment.
        /// Returns true if operation completed (best-effort), false on failure.
        /// </summary>
        Task<bool> SetRolesForStaffAsync(int staffId, IEnumerable<string> roles);
    }
}

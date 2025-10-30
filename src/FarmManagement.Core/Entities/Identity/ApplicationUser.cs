using Microsoft.AspNetCore.Identity;

namespace FarmManagement.Core.Entities.Identity;

public class ApplicationUser : IdentityUser<string>
{
    public ApplicationUser() : base()
    {
    // Ensure Id is initialized when creating instances directly
    if (string.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString();
    }

    public ApplicationUser(string userName) : base(userName)
    {
    // Ensure Id is initialized when creating instances directly
    if (string.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString();
    }

    // Required property to link to Staff
    public int StaffId { get; set; }

    // Navigation property to Staff
    public Staff Staff { get; set; } = null!;
}

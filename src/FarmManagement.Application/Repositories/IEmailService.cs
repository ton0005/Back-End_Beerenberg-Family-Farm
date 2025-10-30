namespace FarmManagement.Application.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
    
    // Send new account notification with temporary password
    Task<bool> SendNewAccountEmailAsync(string toEmail, string temporaryPassword);
}
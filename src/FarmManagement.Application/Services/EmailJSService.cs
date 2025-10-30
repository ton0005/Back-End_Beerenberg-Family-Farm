using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmManagement.Application.Services;

public class EmailJSService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailJSService> _logger;

    public EmailJSService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailJSService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        try
        {
            // Get EmailJS configuration from appsettings
            var serviceId = _configuration["EmailJS:ServiceId"];
            var templateId = _configuration["EmailJS:TemplateId"];
            var publicKey = _configuration["EmailJS:PublicKey"];
            var privateKey = _configuration["EmailJS:PrivateKey"];
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "https://localhost:5173";

            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(templateId) || string.IsNullOrEmpty(publicKey))
            {
                _logger.LogError("EmailJS configuration is missing");
                return false;
            }

            // Create reset URL
            var resetUrl = $"{frontendUrl}/reset-password?token={resetToken}";

            // EmailJS API payload
            var templateParams = new Dictionary<string, object>
            {
                ["to_email"] = toEmail,
                ["user_email"] = toEmail,
                ["email"] = toEmail,
                ["recipient_email"] = toEmail,
                ["to"] = toEmail,
                ["to_name"] = toEmail.Split('@')[0],
                ["reset_token"] = resetToken,
                ["company_name"] = "Beerenberg Family Farm",
                ["expires_in"] = "1 hour"
            };

            var emailData = new
            {
                service_id = serviceId,
                template_id = templateId,
                user_id = publicKey,
                accessToken = privateKey,
                template_params = templateParams
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending password reset email to: {Email}", toEmail);
            _logger.LogDebug("EmailJS payload: {Payload}", json);

            // Send email via EmailJS API
            var response = await _httpClient.PostAsync("https://api.emailjs.com/api/v1.0/email/send", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Password reset email sent successfully to: {Email}", toEmail);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending password reset email to: {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNewAccountEmailAsync(string toEmail, string temporaryPassword)
    {
        try
        {
            var serviceId = _configuration["EmailJS:ServiceId"];
            var templateId = _configuration["EmailJS:NewAccountTemplateId"] ?? _configuration["EmailJS:TemplateId"];
            var publicKey = _configuration["EmailJS:PublicKey"];
            var privateKey = _configuration["EmailJS:PrivateKey"];

            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(templateId) || string.IsNullOrEmpty(publicKey))
            {
                _logger.LogError("EmailJS configuration for new account is missing");
                return false;
            }

            var emailData = new
            {
                service_id = serviceId,
                template_id = templateId,
                user_id = publicKey,
                accessToken = privateKey,
                template_params = new
                {
                    to_email = toEmail,
                    to_name = toEmail.Split('@')[0],
                    temp_password = temporaryPassword,
                    company_name = "Beerenberg Family Farm"
                }
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending new account email to: {Email}", toEmail);

            var response = await _httpClient.PostAsync("https://api.emailjs.com/api/v1.0/email/send", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("New account email sent successfully to: {Email}", toEmail);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send new account email. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending new account email to: {Email}", toEmail);
            return false;
        }
    }
}
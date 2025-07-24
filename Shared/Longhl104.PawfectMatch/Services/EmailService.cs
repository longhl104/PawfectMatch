using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Longhl104.PawfectMatch.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null, string? fromEmail = null);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, string loginUrl);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl);
    Task<bool> SendAdoptionNotificationAsync(string toEmail, string userName, string petName, string shelterName);
    Task<bool> SendShelterRegistrationConfirmationAsync(string toEmail, string shelterName, string approvalUrl);
}

public class EmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _configurationSetName;
    private readonly string _fromDomain;
    private readonly string _defaultFromEmail;

    public EmailService(
        IAmazonSimpleEmailService sesClient,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _sesClient = sesClient;
        _configuration = configuration;
        _logger = logger;

        _configurationSetName = _configuration["SESConfigurationSetName"] ?? "";
        _fromDomain = _configuration["SESFromDomain"] ?? "pawfectmatchnow.com";
        _defaultFromEmail = _configuration["EmailNoreply"] ?? $"noreply@{_fromDomain}";
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null, string? fromEmail = null)
    {
        try
        {
            var fromAddress = fromEmail ?? _defaultFromEmail;

            var sendRequest = new SendEmailRequest
            {
                Source = fromAddress,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body()
                },
                ConfigurationSetName = _configurationSetName
            };

            // Set HTML body
            if (!string.IsNullOrEmpty(htmlBody))
            {
                sendRequest.Message.Body.Html = new Content(htmlBody);
            }

            // Set text body (fallback or provided)
            var textContent = textBody ?? StripHtml(htmlBody);
            if (!string.IsNullOrEmpty(textContent))
            {
                sendRequest.Message.Body.Text = new Content(textContent);
            }

            var response = await _sesClient.SendEmailAsync(sendRequest);

            _logger.LogInformation("Email sent successfully to {ToEmail}. MessageId: {MessageId}",
                toEmail, response.MessageId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, string loginUrl)
    {
        var subject = "Welcome to PawfectMatch!";

        var htmlBody = GenerateWelcomeEmailHtml(userName, loginUrl);
        var fromEmail = _configuration["EmailWelcome"] ?? $"welcome@{_fromDomain}";

        return await SendEmailAsync(toEmail, subject, htmlBody, fromEmail: fromEmail);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl)
    {
        var subject = "Reset Your PawfectMatch Password";

        var htmlBody = GeneratePasswordResetEmailHtml(userName, resetUrl);
        var fromEmail = _configuration["EmailSupport"] ?? $"support@{_fromDomain}";

        return await SendEmailAsync(toEmail, subject, htmlBody, fromEmail: fromEmail);
    }

    public async Task<bool> SendAdoptionNotificationAsync(string toEmail, string userName, string petName, string shelterName)
    {
        var subject = $"New Adoption Application for {petName}";

        var htmlBody = GenerateAdoptionNotificationEmailHtml(userName, petName, shelterName);
        var fromEmail = _configuration["EmailNotifications"] ?? $"notifications@{_fromDomain}";

        return await SendEmailAsync(toEmail, subject, htmlBody, fromEmail: fromEmail);
    }

    public async Task<bool> SendShelterRegistrationConfirmationAsync(string toEmail, string shelterName, string approvalUrl)
    {
        var subject = "Shelter Registration - Pending Approval";

        var htmlBody = GenerateShelterRegistrationEmailHtml(shelterName, approvalUrl);
        var fromEmail = _configuration["EmailAdmin"] ?? $"admin@{_fromDomain}";

        return await SendEmailAsync(toEmail, subject, htmlBody, fromEmail: fromEmail);
    }

    private string GenerateWelcomeEmailHtml(string userName, string loginUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to PawfectMatch</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Welcome to PawfectMatch!</h1>
    </div>
    <div class='content'>
        <h2>Hello {userName}!</h2>
        <p>Welcome to PawfectMatch, where loving families find their perfect furry companions!</p>
        <p>Your account has been successfully created. You can now start browsing available pets and connecting with local shelters.</p>
        <p>Get started by logging into your account:</p>
        <a href='{loginUrl}' class='button'>Login to Your Account</a>
        <p>If you have any questions, feel free to contact our support team.</p>
        <p>Happy pet matching!</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 PawfectMatch. All rights reserved.</p>
        <p>This email was sent from an automated system. Please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetEmailHtml(string userName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF6B35; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #FF6B35; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .warning {{ color: #d32f2f; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Password Reset Request</h1>
    </div>
    <div class='content'>
        <h2>Hello {userName},</h2>
        <p>We received a request to reset your PawfectMatch account password.</p>
        <p>Click the button below to reset your password:</p>
        <a href='{resetUrl}' class='button'>Reset Password</a>
        <p class='warning'>This link will expire in 1 hour for security reasons.</p>
        <p>If you didn't request this password reset, please ignore this email or contact our support team if you have concerns.</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 PawfectMatch. All rights reserved.</p>
        <p>This email was sent from an automated system. Please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    private string GenerateAdoptionNotificationEmailHtml(string userName, string petName, string shelterName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Adoption Application</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .pet-info {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #2196F3; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>New Adoption Application</h1>
    </div>
    <div class='content'>
        <h2>Great News!</h2>
        <p>A potential adopter has submitted an application for one of your pets.</p>
        <div class='pet-info'>
            <strong>Pet:</strong> {petName}<br>
            <strong>Applicant:</strong> {userName}<br>
            <strong>Shelter:</strong> {shelterName}
        </div>
        <p>Please log into your shelter dashboard to review the application and contact the potential adopter.</p>
        <p>Thank you for using PawfectMatch to help find loving homes for pets!</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 PawfectMatch. All rights reserved.</p>
        <p>This email was sent from an automated system. Please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    private string GenerateShelterRegistrationEmailHtml(string shelterName, string approvalUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Shelter Registration Received</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .status {{ background-color: #FFF3E0; padding: 15px; margin: 15px 0; border-left: 4px solid #FF9800; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Registration Received</h1>
    </div>
    <div class='content'>
        <h2>Thank you for registering {shelterName}!</h2>
        <p>We've received your shelter registration application and it's currently under review.</p>
        <div class='status'>
            <strong>Status:</strong> Pending Admin Approval<br>
            <strong>Next Steps:</strong> Our team will review your application within 2-3 business days
        </div>
        <p>You'll receive another email once your application has been approved and your shelter account is activated.</p>
        <p>If you have any questions during the review process, please don't hesitate to contact our support team.</p>
        <p>Thank you for joining the PawfectMatch community!</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 PawfectMatch. All rights reserved.</p>
        <p>This email was sent from an automated system. Please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Simple HTML stripping - for production, consider using HtmlAgilityPack
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Trim();
    }
}

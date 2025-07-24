# PawfectMatch Email Setup Guide

This guide explains how to set up and use email functionality in PawfectMatch using AWS Simple Email Service (SES).

## Overview

The PawfectMatch application now includes comprehensive email services that allow you to send emails using your `@pawfectmatchnow.com` domain. This includes:

- Welcome emails for new users
- Password reset emails
- Adoption notification emails
- Shelter registration confirmation emails
- Custom email sending functionality

## Infrastructure Setup

### 1. AWS SES Domain Configuration

The CDK automatically creates the following SES resources in the `SharedStack`:

- **SES Email Identity**: Verifies your domain for sending emails
- **SES Configuration Set**: Manages email tracking and deliverability
- **DKIM Signing**: Enabled for better email deliverability
- **Custom Mail-From Domain**: Uses `mail.pawfectmatchnow.com` subdomain

### 2. Environment-Specific Domains

- **Production**: `pawfectmatchnow.com`
- **Development**: `development.pawfectmatchnow.com`

### 3. Predefined Email Addresses

The system creates the following email addresses for different purposes:

- `noreply@pawfectmatchnow.com` - Default sender for automated emails
- `support@pawfectmatchnow.com` - Support and password reset emails
- `notifications@pawfectmatchnow.com` - Adoption and notification emails
- `welcome@pawfectmatchnow.com` - Welcome emails for new users
- `admin@pawfectmatchnow.com` - Administrative emails

## Deployment Steps

### 1. Deploy Infrastructure

Deploy the updated CDK stacks to create SES resources:

```bash
# Deploy shared stack first (contains SES configuration)
cd cdk
cdk deploy PawfectMatch-{stage}-Shared

# Deploy other stacks
cdk deploy PawfectMatch-{stage}-Identity
cdk deploy PawfectMatch-{stage}-Matcher
cdk deploy PawfectMatch-{stage}-ShelterHub
```

### 2. Verify Domain in AWS SES

After deployment, you need to verify your domain with AWS SES:

#### Step 1: Get DNS Records
1. Go to AWS SES Console → Verified identities
2. Find your domain (`pawfectmatchnow.com` or `development.pawfectmatchnow.com`)
3. Copy the DNS verification records

#### Step 2: Add DNS Records
Add the following records to your Route 53 hosted zone:

1. **Domain Verification Record** (TXT record)
2. **DKIM Records** (3 CNAME records for DKIM signing)
3. **Mail-From Domain** (MX and TXT records)

Example DNS records:
```
# Domain verification (example - use actual values from SES console)
_amazonses.pawfectmatchnow.com TXT "verification-token-here"

# DKIM records (example - use actual values from SES console)
token1._domainkey.pawfectmatchnow.com CNAME token1.dkim.amazonses.com
token2._domainkey.pawfectmatchnow.com CNAME token2.dkim.amazonses.com
token3._domainkey.pawfectmatchnow.com CNAME token3.dkim.amazonses.com

# Mail-from domain
mail.pawfectmatchnow.com MX 10 feedback-smtp.{region}.amazonses.com
mail.pawfectmatchnow.com TXT "v=spf1 include:amazonses.com ~all"
```

#### Step 3: Wait for Verification
Domain verification can take up to 72 hours, but usually completes within a few hours.

### 3. Request Production Access

By default, AWS SES starts in sandbox mode. To send emails to any address:

1. Go to AWS SES Console → Account dashboard
2. Click "Request production access"
3. Fill out the form explaining your use case
4. Wait for approval (usually 24-48 hours)

## Usage in Applications

### 1. Service Registration

The email service is automatically registered when you add PawfectMatch services:

```csharp
// In Program.cs
builder.Services.AddPawfectMatchEmailServices();
```

### 2. Using the Email Service

Inject `IEmailService` into your controllers or services:

```csharp
public class RegistrationController : ControllerBase
{
    private readonly IEmailService _emailService;

    public RegistrationController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("adopter")]
    public async Task<IActionResult> RegisterAdopter([FromBody] AdopterRegistrationRequest request)
    {
        // ... registration logic ...

        // Send welcome email
        var loginUrl = "https://adopter.pawfectmatchnow.com/auth/login";
        await _emailService.SendWelcomeEmailAsync(
            request.Email, 
            request.FullName, 
            loginUrl
        );

        return Ok();
    }
}
```

### 3. Available Email Methods

#### Send Custom Email
```csharp
await _emailService.SendEmailAsync(
    toEmail: "user@example.com",
    subject: "Custom Subject",
    htmlBody: "<h1>HTML Content</h1>",
    textBody: "Plain text fallback", // optional
    fromEmail: "custom@pawfectmatchnow.com" // optional
);
```

#### Send Welcome Email
```csharp
await _emailService.SendWelcomeEmailAsync(
    toEmail: "newuser@example.com",
    userName: "John Doe",
    loginUrl: "https://adopter.pawfectmatchnow.com/auth/login"
);
```

#### Send Password Reset Email
```csharp
await _emailService.SendPasswordResetEmailAsync(
    toEmail: "user@example.com",
    userName: "John Doe",
    resetUrl: "https://id.pawfectmatchnow.com/auth/reset?token=..."
);
```

#### Send Adoption Notification
```csharp
await _emailService.SendAdoptionNotificationAsync(
    toEmail: "shelter@example.com",
    userName: "John Doe",
    petName: "Buddy",
    shelterName: "Happy Paws Shelter"
);
```

#### Send Shelter Registration Confirmation
```csharp
await _emailService.SendShelterRegistrationConfirmationAsync(
    toEmail: "shelter@example.com",
    shelterName: "Happy Paws Shelter",
    approvalUrl: "https://admin.pawfectmatchnow.com/approvals"
);
```

## Configuration

### AWS Systems Manager Parameters

The email service uses the following SSM parameters (automatically created by CDK):

- `/PawfectMatch/{Stage}/Common/SESConfigurationSetName`
- `/PawfectMatch/{Stage}/Common/SESFromDomain`
- `/PawfectMatch/{Stage}/Common/EmailNoreply`
- `/PawfectMatch/{Stage}/Common/EmailSupport`
- `/PawfectMatch/{Stage}/Common/EmailNotifications`
- `/PawfectMatch/{Stage}/Common/EmailWelcome`
- `/PawfectMatch/{Stage}/Common/EmailAdmin`

### Environment Variables

You can also configure email settings via environment variables:

```bash
export SESConfigurationSetName="pawfectmatch-production"
export SESFromDomain="pawfectmatchnow.com"
export EmailNoreply="noreply@pawfectmatchnow.com"
export EmailSupport="support@pawfectmatchnow.com"
# ... etc
```

## Email Templates

### Template Styling

All email templates use responsive HTML with inline CSS for maximum compatibility:

- Clean, professional design
- Mobile-friendly responsive layout
- Consistent branding with PawfectMatch colors
- Clear call-to-action buttons
- Professional footer with unsubscribe information

### Customizing Templates

To customize email templates, modify the methods in `EmailService.cs`:

- `GenerateWelcomeEmailHtml()`
- `GeneratePasswordResetEmailHtml()`
- `GenerateAdoptionNotificationEmailHtml()`
- `GenerateShelterRegistrationEmailHtml()`

## Security and Best Practices

### 1. Email Security
- All emails use DKIM signing for authenticity
- SPF records prevent spoofing
- TLS encryption in transit
- Rate limiting through SES configuration sets

### 2. Error Handling
- All email sending methods return `bool` for success/failure
- Detailed logging for debugging
- Graceful degradation if email service is unavailable

### 3. Performance
- Asynchronous email sending
- Connection pooling through AWS SDK
- Retry logic built into AWS SDK

## Monitoring and Troubleshooting

### 1. AWS SES Metrics

Monitor email sending through AWS CloudWatch:
- Send rate
- Bounce rate
- Complaint rate
- Delivery rate

### 2. Application Logs

The email service logs all sending attempts:

```csharp
// Success
_logger.LogInformation("Email sent successfully to {ToEmail}. MessageId: {MessageId}", 
    toEmail, response.MessageId);

// Failure
_logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
```

### 3. Common Issues

#### Domain Not Verified
- **Error**: `MessageRejected: Email address not verified`
- **Solution**: Complete domain verification in SES console

#### Sandbox Mode
- **Error**: Email only sends to verified addresses
- **Solution**: Request production access in SES console

#### Rate Limiting
- **Error**: `Throttling: Rate exceeded`
- **Solution**: Implement exponential backoff or request rate increase

#### DNS Issues
- **Error**: DKIM verification fails
- **Solution**: Verify all DNS records are correctly configured

## Testing

### 1. Development Testing

In development, you can test with verified email addresses or use the SES mailbox simulator:

```csharp
// Test addresses (always work in sandbox mode)
await _emailService.SendEmailAsync("success@simulator.amazonses.com", "Test", "Body");
await _emailService.SendEmailAsync("bounce@simulator.amazonses.com", "Test", "Body");
await _emailService.SendEmailAsync("complaint@simulator.amazonses.com", "Test", "Body");
```

### 2. Production Testing

Test the complete flow:

1. Register a new user
2. Check welcome email delivery
3. Test password reset functionality
4. Submit adoption application
5. Verify notification emails

## Cost Considerations

AWS SES pricing (as of 2025):
- First 62,000 emails per month: $0.10 per 1,000 emails
- Additional emails: $0.10 per 1,000 emails
- No setup fees or monthly commitments
- Data transfer charges may apply

## Future Enhancements

Consider implementing:

1. **Email Templates Engine**: Use external template service like SendGrid or Amazon Pinpoint
2. **Email Analytics**: Track open rates, click rates, and user engagement
3. **Unsubscribe Management**: Allow users to manage email preferences
4. **Email Scheduling**: Queue emails for optimal delivery times
5. **A/B Testing**: Test different email content and subject lines

## Support

For issues related to email functionality:

1. Check AWS SES console for domain verification status
2. Review application logs for error messages
3. Verify DNS configuration in Route 53
4. Test with SES mailbox simulator addresses
5. Contact AWS support for SES-specific issues

This completes the email setup for PawfectMatch. Users can now receive emails from your `@pawfectmatchnow.com` domain for all application notifications and communications.

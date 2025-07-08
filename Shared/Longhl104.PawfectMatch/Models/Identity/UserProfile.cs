namespace Longhl104.PawfectMatch.Models.Identity;

public class UserProfile
{
    public required Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

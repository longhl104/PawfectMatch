namespace Longhl104.Matcher.Models;

public class CreateAdopterRequest
{
    public required string UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

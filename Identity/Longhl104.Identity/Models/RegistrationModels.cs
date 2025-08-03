namespace Longhl104.Identity.Models;

public class AdopterRegistrationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class AdopterRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public TokenData? Data { get; set; }
}

public class ShelterAdminRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ShelterName { get; set; } = string.Empty;
    public string ShelterContactNumber { get; set; } = string.Empty;
    public string ShelterAddress { get; set; } = string.Empty;
    public string? ShelterWebsiteUrl { get; set; }
    public string? ShelterAbn { get; set; }
    public string? ShelterDescription { get; set; }
    public decimal? ShelterLatitude { get; set; }
    public decimal? ShelterLongitude { get; set; }
}

public class ShelterAdminRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public TokenData? Data { get; set; }
}

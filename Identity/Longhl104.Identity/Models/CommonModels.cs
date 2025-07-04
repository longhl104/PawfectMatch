namespace Longhl104.Identity.Models;

/// <summary>
/// Represents a user profile in the system
/// </summary>
public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

/// <summary>
/// Contains token data for authentication
/// </summary>
public class TokenData
{
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty; // OIDC ID Token
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfile? User { get; set; }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// Request model for user authentication
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response model for login operations
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TokenData? Data { get; set; }
}

/// <summary>
/// Request model for refresh token operations
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for refresh token operations
/// </summary>
public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TokenData? Data { get; set; }
}

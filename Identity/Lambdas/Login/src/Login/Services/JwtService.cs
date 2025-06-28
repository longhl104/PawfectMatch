using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Login.Models;

namespace Login.Services;

public interface IJwtService
{
    string GenerateAccessToken(UserProfile user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    bool ValidateRefreshToken(string refreshToken, string userId);
}

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public JwtService()
    {
        _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev-jwt-secret-key";
        _accessTokenExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN") ?? "60");
        _refreshTokenExpiryDays = int.Parse(Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRES_IN") ?? "30");
    }

    public string GenerateAccessToken(UserProfile user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Email, user.Email),
            new("user_type", user.UserType),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            Issuer = "PawfectMatch",
            Audience = "PawfectMatch-App",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "PawfectMatch",
                ValidateAudience = true,
                ValidAudience = "PawfectMatch-App",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateRefreshToken(string refreshToken, string userId)
    {
        // In a production system, you would store refresh tokens in a database
        // and validate against that. For now, we'll do basic validation.
        // This should be enhanced to check against a refresh token store.
        return !string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(userId);
    }
}

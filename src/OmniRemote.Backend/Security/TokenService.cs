using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OmniRemote.Backend.Security;

public sealed class TokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(IConfiguration configuration)
    {
        var rawKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 32)
        {
            throw new InvalidOperationException("CRITICAL CONFIGURATION ERROR: 'Jwt:SigningKey' must be at least 32 characters long. The backend refuses to start with missing or weak credentials.");
        }

        _issuer = configuration["Jwt:Issuer"] ?? "omniremote-backend";
        _audience = configuration["Jwt:Audience"] ?? "omniremote-mobile";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
    }

    public string CreateToken(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

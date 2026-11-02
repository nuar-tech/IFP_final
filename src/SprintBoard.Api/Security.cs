// src/SprintBoard.Api/Security.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SprintBoard.Infrastructure;

namespace SprintBoard.Api;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Key { get; set; } = "";
}

public sealed class TokenService(IOptions<JwtOptions> options)
{
    public string Create(AppUser user)
    {
        var jwt = options.Value;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email ?? ""),
            new Claim("name", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            jwt.Issuer, jwt.Audience, claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Missing sub claim."));
}
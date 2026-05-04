using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MentoringApp.Model.User;
using Microsoft.IdentityModel.Tokens;

namespace MentoringApp.Api.Helpers;

public static class JwtHelper
{
    public static (string token, DateTime expiresAt) GenerateToken(UserModel user, JwtSettings settings)
    {
        var role = user switch
        {
            AdminModel => "Admin",
            SupervisorModel => "Supervisor",
            StudentModel => "Student",
            _ => "Student"
        };

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim("role", role),
            new Claim("language", user.Language),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(settings.ExpiryHours);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Claims;

namespace Auth.Infrastructure.Authentication;

public class TokenGenerator : ITokenGenerator
{
    private readonly TokenSettings _tokenSettings;

    public TokenGenerator(IOptions<TokenSettings> tokenSettings)
    {
        _tokenSettings = tokenSettings.Value;
        if (string.IsNullOrEmpty(_tokenSettings.SecretKey))
        {
            throw new ArgumentException("TokenSettings.SecretKey no está configurado");
        }
    }
    public string GenerateAccessToken(int userId)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> {
                    new Claim (ClaimTypes.NameIdentifier, userId.ToString()),
                };
          
        var token = new JwtSecurityToken(
            claims:claims,
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            expires: DateTime.UtcNow.AddMinutes(_tokenSettings.ExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public int GetAccessTokenExpirationMinutes() 
        => _tokenSettings.ExpirationMinutes;
}

using System;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Shared.Result;

namespace Auth.Infrastructure.Authentication;

public interface IGoogleTokenValidator
{
    Task<Result<GoogleUserInfo>> ValidateTokenAsync(string idToken);
}
public class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    public async Task<Result<GoogleUserInfo>> ValidateTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { configuration["Authentication:Google:ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var userInfo = new GoogleUserInfo
            {
                Email = payload.Email,
                Name = payload.Name,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Picture = payload.Picture,
                GoogleId = payload.Subject,
                EmailVerified = payload.EmailVerified
            };

            return userInfo;
        }
        catch (Exception ex)
        {
            return new Error("INVALID_TOKEN", $"Token de Google inválido: {ex.Message}"); // check if this is ok
        }
    }
}
public class GoogleUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string GoogleId { get; set; } =string.Empty;
    public bool EmailVerified { get; set; } = true;
}
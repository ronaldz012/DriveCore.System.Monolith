using System;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared.Result;

namespace Auth.Infrastructure.Authentication;

public interface IGoogleTokenValidator
{
    Task<Result<GoogleUserInfo>> ValidateTokenAsync(string idToken);
}
public class GoogleTokenValidator(IOptions<AuthenticationSettings> AuthSettings) : IGoogleTokenValidator
{
    private readonly Google googleConfig = AuthSettings.Value.Google;
    public async Task<Result<GoogleUserInfo>> ValidateTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleConfig.ClientId}
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
        catch (InvalidJwtException ex) 
        {
            // El token no es auténtico, expiró o no es para esta app.
            return new Error("GOOGLE_TOKEN_INVALID", $"El ID Token de Google no es válido: {ex.Message}");
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
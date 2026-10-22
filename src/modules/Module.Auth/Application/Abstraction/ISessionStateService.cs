using Common.Contracts.authentication;
using Common.Contracts.authentication.dtos;
using Common.Utilities;

namespace Module.Auth.Application.Abstraction;

public interface ISessionStateService
{
    Task<Result<AuthenticatedSessionDto>> AuthenticateByExternalIdAsync(string externalAuthId);
    Result<SessionStateDto> GetSessionAsync();
    Result<ActorContext> GetActorContext();

    /// <summary>
    /// Permission probe for field-level access, evaluated against every active
    /// branch (same X-Branch-Id resolution as GetActorContext).
    /// Returns Success(true) / Success(false) when the session is valid, or an
    /// Error (Unauthorized / BadRequest / Forbidden) when access cannot be checked.
    /// </summary>
    Result<bool> HasPermission(string feature, string permission);

    void Invalidate(string externalAuthId);
    void InvalidateTenant(Guid tenantId);
}

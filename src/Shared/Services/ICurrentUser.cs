using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Services;

public interface ICurrentUser
{
    int UserId { get; }
    string Username { get; }
    string? Token { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<int> BranchIds { get; }
    bool HasBranch(int branchId);
}
public class CurrentUserService : ICurrentUser
{
    // Campos calculados una sola vez
    public int UserId { get; }
    public string Username { get; }
    public string? Token { get; }
    public bool IsAuthenticated { get; }
    public IReadOnlyList<int> BranchIds { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        var user = context?.User;

        // 1. Datos de Identidad
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
        
        UserId = IsAuthenticated 
            ? int.Parse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0") 
            : 0;

        Username = user?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";

        // 2. Token
        Token = context?.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        // 3. Sucursales (Extraemos el string del header una vez)
        var headerValues = context?.Request.Headers["X-Branch-Id"].ToString();

        BranchIds = string.IsNullOrWhiteSpace(headerValues)
            ? [] 
            : headerValues.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList()
                .AsReadOnly();
    }

    public bool HasBranch(int branchId) => BranchIds.Contains(branchId);
}
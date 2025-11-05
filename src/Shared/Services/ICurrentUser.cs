using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
namespace Shared.Services;

public interface ICurrentUser
{
    int UserId { get; }
    string Username { get; }
    //IEnumerable<string> Roles { get; }
    //IEnumerable<(string Role, string Permission)> Permissions { get; }
    public string? Token { get; }
    bool IsAuthenticated { get; }
}


public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{

    private ClaimsPrincipal User => httpContextAccessor.HttpContext.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public int UserId => IsAuthenticated
    ? int.Parse(User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "0")
    : 0;

    public string Username => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Anonymous";
    public string? Token => httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
    // public IEnumerable<string> Roles =>
    //     User?.Claims
    //         .Where(c => c.Type == "r")
    //         .Select(c => c.Value)
    //         ?? Enumerable.Empty<string>();

    // public IEnumerable<(string Role, string Permission)> Permissions =>
    //     User?.Claims
    //         .Where(c => c.Type == "p")
    //         .Select(c =>
    //         {
    //             var parts = c.Value.Split(':');
    //             return (Role: parts[0], Permission: parts[1]);
    //         })
    //         ?? Enumerable.Empty<(string, string)>();
}

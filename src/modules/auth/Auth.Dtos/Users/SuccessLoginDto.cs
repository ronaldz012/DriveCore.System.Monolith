using System;
using Auth.Dtos.Modules;
using Auth.Dtos.Roles;

namespace Auth.Dtos.Users;

public class SuccessLoginDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = string.Empty;
    public UserDetailsDto User { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
    public List<ModulePermissionsDeductedDto> Modules { get; set; } = new();
}

public class ModulePermissionsDeductedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public IEnumerable<MenuDto> Menus  { get; set; } = Enumerable.Empty<MenuDto>();
}
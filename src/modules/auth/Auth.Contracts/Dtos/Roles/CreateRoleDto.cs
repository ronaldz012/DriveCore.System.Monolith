namespace Auth.Contracts.Dtos.Roles;

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<RoleModulePermissionDto> RoleModulePermissions { get; set; } = Enumerable.Empty<RoleModulePermissionDto>();
    
}

public class RoleModulePermissionDto
{
    public int ModuleId { get; set; }
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}

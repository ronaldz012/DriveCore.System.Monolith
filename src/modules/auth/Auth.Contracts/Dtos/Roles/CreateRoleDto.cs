namespace Auth.Contracts.Dtos.Roles;

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<RoleFeaturePermissionDto> RoleModulePermissions { get; set; } = Enumerable.Empty<RoleFeaturePermissionDto>();
    
}

public class RoleFeaturePermissionDto
{
    public int FeatureId { get; set; }
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}

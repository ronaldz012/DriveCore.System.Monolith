namespace Auth.Contracts.Dtos.Roles;

public class RoleDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<ModulePermissionsDto> ModulePermissions { get; set; } = Enumerable.Empty<ModulePermissionsDto>();
    
}

public class ModulePermissionsDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
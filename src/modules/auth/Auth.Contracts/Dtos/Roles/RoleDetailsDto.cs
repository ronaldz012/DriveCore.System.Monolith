namespace Auth.Contracts.Dtos.Roles;

public class RoleDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<FeaturePermissionsDto> FeaturePermissions { get; set; } = Enumerable.Empty<FeaturePermissionsDto>();
    
}

public class FeaturePermissionsDto
{
    public int FeatureId { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
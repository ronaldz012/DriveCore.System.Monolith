namespace Auth.Contracts.Dtos.Users;

public class SuccessLoginDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = string.Empty;
    public UserDetailsDto User { get; set; } = default!;
    public List<BranchAccessDto> Branches { get; set; } = new List<BranchAccessDto>();

}

public class BranchAccessDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<RoleDto> Roles { get; set; } = [];
    public List<FeaturePermissionsDeductedDto> Features { get; set; } = [];
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public class FeaturePermissionsDeductedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanCreate { get; set; } = false;
    public bool CanRead { get; set; } = false;
    public bool CanUpdate { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
namespace Auth.Contracts.Dtos.Users;

public class CompleteUserRoleDto
{
    public string RoleType { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // other required columns :::: driver: licences number for example
}

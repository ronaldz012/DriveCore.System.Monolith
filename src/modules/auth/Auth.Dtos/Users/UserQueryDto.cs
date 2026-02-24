using Shared.Extensions;

namespace Auth.Dtos.Users;

public class UserQueryDto :GenericPaginationQueryDto
{
    public string? Email { get; set; }
}
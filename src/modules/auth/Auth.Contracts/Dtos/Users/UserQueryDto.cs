using Shared.Extensions;

namespace Auth.Contracts.Dtos.Users;

public class UserQueryDto :GenericPaginationQueryDto
{
    public string? Email { get; set; }
}
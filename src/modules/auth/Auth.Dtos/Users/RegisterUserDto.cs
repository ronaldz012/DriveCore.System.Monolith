using System;
using System.ComponentModel.DataAnnotations;

namespace Auth.Dtos.Users;

/// <summary>
/// Quick Register :D
/// </summary>
public class RegisterUserDto
{
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
}

public class RegisterUserByAdminDto : RegisterUserDto
{
        public IEnumerable<int> RoleIds { get; set; } = Enumerable.Empty<int>();
}


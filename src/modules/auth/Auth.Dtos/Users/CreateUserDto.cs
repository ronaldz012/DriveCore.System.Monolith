using System.ComponentModel.DataAnnotations;

namespace Auth.Dtos.Users;

public class CreateUserDto
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required, MinLength(3), MaxLength(20)]
    public string Password { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.MinValue;
    
    public IEnumerable<int> RoleIds { get; set; } = new List<int>();
    public IEnumerable<int> BranchIds { get; set; } = new List<int>();
}
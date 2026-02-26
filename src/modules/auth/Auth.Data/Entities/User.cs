using System.ComponentModel.DataAnnotations;
using Shared.Domain;
namespace Auth.Data.Entities;

public class User : ICreatedAt, ICreatedBy, IUpdatedAt, ISoftDelete, IUpdatedBy
{
    [Key]
    public int Id { get; set; }
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    [StringLength(15)]
    public string Ci { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.MinValue;
    public UserStatus Status { get; set; } 
    public string? GoogleId { get; set; } 
    public DateTime LastActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public int? DeletedBy { get; set; }
    public int CreatedBy { get; set; }

    public AuthProvider AuthProvider { get; set; }
    public string? ExternalAuthId { get; set; } 

    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserBranch> UserBranches { get; set; } = [];
    public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } = [];
}

public enum UserStatus
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Inactive = 3,
    PendingRoleSelecting =4
}

public enum AuthProvider
{
    Local = 0,
    Google = 1,
    Facebook = 2,
    Microsoft = 3
}   
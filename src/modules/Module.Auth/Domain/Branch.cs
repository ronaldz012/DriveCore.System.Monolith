using Common.Domain;
using Module.Auth.Domain;

namespace Module.Auth.Domain;

public class Branch : IMustHaveTenant, ICreatedAt, ICreatedBy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string BranchCode { get; set; } = string.Empty;
    public BranchType Type { get; set; } = BranchType.Warehouse;
    public List<string> AllowedFeatureKeys { get; set; } = [];
    public DateTime? CompleteSince { get; set; }

    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public ICollection<UserBranchRole> UserBranchRoles { get; set; } = new List<UserBranchRole>();
    public Tenant Tenant { get; set; } = null!;

    public static Branch Create(Guid id, string name, string place, string phoneNumber, BranchType type, Guid createdBy, string createdByName)
    {
        return new Branch
        {
            Id = id,
            Name = name,
            Place = place,
            PhoneNumber = phoneNumber,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
        };
    }

    public bool CanDeactivate()
    {
        return IsActive;
    }

    public void Deactivate()
    {
        if (!CanDeactivate())
            throw new InvalidOperationException($"Branch {Id} is already inactive.");
        IsActive = false;
    }

    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException($"Branch {Id} is already active.");
        IsActive = true;
    }

    public void UpdateDetails(string name, string place, string phoneNumber, string branchCode)
    {
        Name = name;
        Place = place;
        PhoneNumber = phoneNumber;
        BranchCode = branchCode;
    }

    /// <summary>
    /// Marca la branch como completa. Operación sin retorno: no se puede reabrir la transición.
    /// </summary>
    public void MarkComplete(DateTime completeSince)
    {
        CompleteSince = completeSince;
    }
}

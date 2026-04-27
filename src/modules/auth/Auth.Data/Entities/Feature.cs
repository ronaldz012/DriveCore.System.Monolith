using System;
using Shared.Domain;

namespace Auth.Data.Entities;

public class Feature : ICreatedAt, IUpdatedAt
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    //Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public ICollection<RoleFeaturePermission> RoleFeaturePermissions { get; set; } = default!;
}
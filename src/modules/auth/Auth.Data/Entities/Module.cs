using System;
using Shared.Domain;

namespace Auth.Data.Entities;

public class Module : ICreatedAt, IUpdatedAt
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    //Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<RoleModulePermission> RoleModulePermissions { get; set; } = default!;
    public ICollection<Menu> Menus { get; set; } = default!;
}
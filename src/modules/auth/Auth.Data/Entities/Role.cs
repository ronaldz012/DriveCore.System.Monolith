using System;
using Shared.Domain;

namespace Auth.Data.Entities;

public class Role : ICreatedAt, ISoftDelete, ICreatedBy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Public { get; set; } = false;

        //Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
        public int CreatedBy { get; set; }  
        public int? DeletedBy { get; set; }

    //Navigation property
    public ICollection<UserBranchRole> UserRoles { get; set; } = default!;
    public ICollection<RoleFeaturePermission> RoleFeaturePermissions { get; set; } = default!;

}

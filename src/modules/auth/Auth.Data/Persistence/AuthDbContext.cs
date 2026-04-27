using System;
using Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserBranchRole> UserBranchRoles { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<RoleFeaturePermission> RoleFeaturePermissions { get; set; }

        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasMany(u => u.EmailVerificationCodes)
                  .WithOne(evc => evc.User)
                  .HasForeignKey(evc => evc.UserId);
        });
        modelBuilder.Entity<Role>(entity =>
        {
        });
        modelBuilder.Entity<UserBranchRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserBranchRoles)
                  .HasForeignKey(ur => ur.UserId);

            entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            
            
        });
        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasOne(x => x.Module)
                .WithMany(m => m.Features)
                .HasForeignKey(x => x.ModuleId);
        });
        modelBuilder.Entity<RoleFeaturePermission>(entity =>
        {
            entity.HasOne(rmp => rmp.Role)
                  .WithMany(r => r.RoleFeaturePermissions)
                  .HasForeignKey(rmp => rmp.RoleId);

            entity.HasOne(rmp => rmp.Feature)
                  .WithMany(m => m.RoleFeaturePermissions)
                  .HasForeignKey(rmp => rmp.FeatureId);
        });
        
    }

}

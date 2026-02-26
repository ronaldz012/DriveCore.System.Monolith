using System;
using Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<RoleModulePermission> RoleModulePermissions { get; set; }
        public DbSet<Menu> Menus { get; set; }

        public DbSet<UserBranch> UserBranches { get; set; }
        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasMany(u => u.EmailVerificationCodes)
                  .WithOne(evc => evc.User)
                  .HasForeignKey(evc => evc.UserId);
            
            entity.HasMany(u => u.UserBranches)
                  .WithOne(b => b.User)
                  .HasForeignKey(b => b.UserId);
        });
        modelBuilder.Entity<Role>(entity =>
        {
        });
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId);

            entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
        });
        modelBuilder.Entity<Module>(entity =>
        {
        });
        modelBuilder.Entity<RoleModulePermission>(entity =>
        {
            entity.HasOne(rmp => rmp.Role)
                  .WithMany(r => r.RoleModulePermissions)
                  .HasForeignKey(rmp => rmp.RoleId);

            entity.HasOne(rmp => rmp.Module)
                  .WithMany(m => m.RoleModulePermissions)
                  .HasForeignKey(rmp => rmp.ModuleId);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasOne(m => m.Module)
                  .WithMany(mod => mod.Menus)
                  .HasForeignKey(m => m.ModuleId);
        });
    }

}

using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Module.Auth.Infrastructure.Persistence;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();

        var basePath = Path.Combine(currentDir, "..", "System.Api");
        if (!Directory.Exists(basePath))
            basePath = currentDir;

        // Design-time tooling uses the BASE appsettings.json only: the
        // appsettings.Development.json override belongs to the running
        // application, not to EF tooling ('dotnet ef' would otherwise
        // silently target whatever database Development configures).
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(basePath))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=erp_db;Username=postgres;Password=P@ssword123";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connectionString, x =>
                x.MigrationsHistoryTable("__EFMigrationsHistory_shared"))
            .Options;

        return new AuthDbContext(options, new DesignTimeTenantConnectionContext());
    }
}

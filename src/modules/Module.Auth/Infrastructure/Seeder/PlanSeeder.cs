using Common.Contracts.Seeder;
using Common.Contracts.inventory;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;
using Module.Auth.Domain;

namespace Module.Auth.Infrastructure.Seeder;

public class PlanSeeder(IAuthDbContext context) : IDataSeeder
{
    public int Order => 3;

    public async Task SeedAsync()
    {
        if (await context.Plans.AnyAsync()) return;

        // 1. Cargamos las features mapeadas por su Key
        var featureMap = await context.Features.ToDictionaryAsync(f => f.Key);

        var basic = new Plan
        {
            Id            = Guid.NewGuid(),
            Name          = "Basic",
            Description   = "Plan básico con roles predefinidos para gestión de ventas e inventario",
            Price         = 150,
            MaxUsers      = 5,
            MaxBranches   = 3,
            MaxExtraRoles = 1,
            AllowedFeatureKeys = ["products", "receptions","transfers","pos","sales","closures"],
            DefaultRolesTemplate =
            [
                new DefaultRoleTemplate
                {
                    Name        = "Vendedor",
                    Description = "Gestión de ventas y consulta de inventario",
                    Permissions =
                    [
                        new() { FeatureKey = "products",  Actions = ["read"] },
                        new() { FeatureKey = "transfers", Actions = ["read", "create", "update"] },
                        new() { FeatureKey = "pos",       Actions = ["read", "create"] },
                        new() { FeatureKey = "sales",     Actions = ["read"] },
                        new() { FeatureKey = "closures",  Actions = ["create"] }
                    ]
                },
                new DefaultRoleTemplate
                {
                    Name        = "Almacenero",
                    Description = "Gestión de stock, recepciones y transferencias",
                    Permissions =
                    [
                        new() { FeatureKey = "products",   Actions = ["read", "create", "update"] },
                        new() { FeatureKey = "receptions", Actions = ["read", "create", "update", "delete"] },
                        new() { FeatureKey = "transfers",  Actions = ["read", "create", "update", "delete"] }
                    ]
                },
                new DefaultRoleTemplate
                {
                    Name        = "Supervisor",
                    Description = "Acceso operativo total",
                    Permissions =
                    [
                        new() { FeatureKey = "products",   Actions = ["read", "create", "update", "delete"] },
                        new() { FeatureKey = "transfers",  Actions = ["read", "create", "update", "delete"] },
                        new() { FeatureKey = "receptions", Actions = ["read", "create", "update", "delete"] },
                        new() { FeatureKey = "pos",        Actions = ["read", "create", "update"] },
                        new() { FeatureKey = "sales",      Actions = ["read", "margin"] },
                        new() { FeatureKey = "closures",   Actions = ["read", "create", "update"] }
                    ]
                },
            ],
            DefaultCatalogTemplate = DefaultCatalogTemplates.Basic
        };

        foreach (var role in basic.DefaultRolesTemplate)
        {
            foreach (var perm in role.Permissions)
            {
                var dbFeature = featureMap[perm.FeatureKey]; 

                foreach (var action in perm.Actions)
                {
                    if (!dbFeature.AvailableActions.Any(a => a.Key == action))
                    {
                        throw new Exception($"Error en Seeder: La acción '{action}' no es válida para la feature '{perm.FeatureKey}' en el rol '{role.Name}'.");
                    }
                }
            }
        }

        await context.Plans.AddAsync(basic);
        await context.SaveChangesAsync();
    }
}
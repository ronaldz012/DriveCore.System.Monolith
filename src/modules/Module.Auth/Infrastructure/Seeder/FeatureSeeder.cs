using Common.Contracts.Seeder;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;
using Module.Auth.Domain;

namespace Module.Auth.Infrastructure.Seeder;

public class FeatureSeeder(IAuthDbContext context) : IDataSeeder
{
    public int Order => 2;

    public async Task SeedAsync()
    {
        if (await context.Features.AnyAsync()) return;

        // Lista genérica de acciones base para reutilizar en los módulos estándar
        List<FeatureAction> GetGenericCrudActions(string featureName) =>
        [
            new() { Key = "read", DisplayName = "Ver", Description = $"Permite listar y ver el detalle de {featureName}." },
            new() { Key = "create", DisplayName = "Crear", Description = $"Permite registrar nuevos {featureName}." },
            new() { Key = "update", DisplayName = "Editar", Description = $"Permite modificar {featureName} existentes." },
            new() { Key = "delete", DisplayName = "Eliminar", Description = $"Permite dar de baja o eliminar {featureName} del sistema." }
        ];

        var features = new List<Feature>
        {
            // ── Inventory ──────────────────────────────────────
            new()
            {
                Key         = "products",
                DisplayName = "Productos",
                Route       = "/products",
                Description = "Manage products catalog",
                Icon        = "inventory_2",
                Module      = Domain.Module.Inventory,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions = GetGenericCrudActions("productos")
            },
            new()
            {
                Key         = "transfers",
                DisplayName = "Transferencias",
                Route       = "/transfers",
                Description = "Manage stock transfers between locations",
                Icon        = "swap_horiz",
                Module      = Domain.Module.Inventory,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions = GetGenericCrudActions("transferencias")
            },
            new()
            {
                Key         = "receptions",
                DisplayName = "Recepciones",
                Route       = "/receptions",
                Description = "Manage incoming stock receptions",
                Icon        = "move_to_inbox",
                Module      = Domain.Module.Inventory,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions = GetGenericCrudActions("recepciones")
            },

            // ── Sales ──────────────────────────────────────────
            new()
            {
                Key         = "pos",
                DisplayName = "POS",
                Route       = "/pos",
                Description = "Process sales at point of sale",
                Icon        = "point_of_sale",
                Module      = Domain.Module.Sales,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions = [
                    new() { Key = "read", DisplayName = "Acceso al POS", Description = "Permite abrir la pantalla de caja y buscar productos." },
                    new() { Key = "create", DisplayName = "Procesar Venta", Description = "Permite cobrar y emitir comprobantes de venta tambien crear gastos (movimientos)" },
                    new() { Key = "update", DisplayName = "Modificar Movimientos", Description = "Permite editar movimietnso (Gastyos)" },
                    new() { Key = "delete", DisplayName = "eliminar movimientos", Description = "Permite elmiar movimientos" }

                ]
            },
            new()
            {
                Key         = "sales",
                DisplayName = "Ventas",
                Route       = "/sales",
                Description = "View and manage sales records",
                Icon        = "receipt",
                Module      = Domain.Module.Sales,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions = [
                    new() { Key = "read", DisplayName = "Ver Ventas", Description = "Permite listar el historial de ventas realizadas." },
                    new() { Key = "margin", DisplayName = "Ver Márgenes", Description = "Permite ver el precio y costo con margen en el detalle de ventas." }
                ]
            },
            new()
            {
                Key         = "closures",
                DisplayName = "Cierres de Caja",
                Route       = "/closures",
                Description = "View and manage cash register closures",
                Icon        = "lock",
                Module      = Domain.Module.Sales,
                IsMenu = true,
                CreatedAt   = DateTime.UtcNow,
                AvailableActions =
                [
                    new() { Key = "read",   DisplayName = "Ver",     Description = "Permite listar y ver detalle de cierres de caja." },
                    new() { Key = "create", DisplayName = "Abrir Caja", Description = "Permite iniciar el turno de caja." },
                    new() { Key = "update", DisplayName = "Cerrar Caja", Description = "Permite confirmar el cierre de la caja." }
                ]
            },
        };

        await context.Features.AddRangeAsync(features);
        await context.SaveChangesAsync();
    }
}
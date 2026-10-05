using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Application.UseCases.Transfers.GetById;

public class StockTransferDetails(IInvDbContext context, IBranchService branchService, IUserIntegrationService userService)
{
    public async Task<Result<StockTransferDetailDto>> Execute(ActorContext ctx, Guid stockTransferId)
    {
        var currentBranchIds = ctx.BranchIds[0];
        var transfer = await context.StockTransfers
            .Include(st => st.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(st => st.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(pv => pv.Color)
            .Include(st => st.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(pv => pv.Size)
            .FirstOrDefaultAsync(x => x.Id == stockTransferId);

        if (transfer == null)
            return StockTransferDetailsErrors.TransferNotFound;

        List<Guid> branchIds = [transfer.FromBranchId, transfer.ToBranchId];
        List<Guid> userIds = transfer.ResolvedByUserId.HasValue
            ? [transfer.RequestedByUserId, transfer.ResolvedByUserId.Value]
            : [transfer.RequestedByUserId];

        var branchesResult = await branchService.GetBranchesByIds(branchIds);
        if (!branchesResult.IsSuccess)
            return StockTransferDetailsErrors.BranchesNotFound;

        var usersResult = await userService.GetUsersByIds(userIds);
        if (!usersResult.IsSuccess)
            return StockTransferDetailsErrors.UsersNotFound;

        var branches = branchesResult.Value.ToDictionary(b => b.Id, b => b.Name);
        var users = usersResult.Value.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var direction = transfer.FromBranchId == currentBranchIds ? TransferDirection.Outbound : TransferDirection.Inbound;

        return new StockTransferDetailDto
        {
            Id = transfer.Id,
            Number = transfer.Number,
            Direction = direction,
            FromBranchName = branches.GetValueOrDefault(transfer.FromBranchId) ?? "Unknown",
            ToBranchName = branches.GetValueOrDefault(transfer.ToBranchId) ?? "Unknown",
            RequesterName = users.GetValueOrDefault(transfer.RequestedByUserId) ?? "Unknown",
            ResolverName = users.GetValueOrDefault(transfer.ResolvedByUserId ?? Guid.Empty) ?? "Unknown",
            Status = transfer.Status,
            Notes = transfer.Notes,
            CreatedAt = transfer.CreatedAt,
            ResolvedAt = transfer.ResolvedAt,
            Items = transfer.Items.Select(i => new StockTransferItemDetailDto
            {
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant.Product.Name,
                VariantDescription = i.ProductVariant.Description,
                Sku = i.ProductVariant.Sku,
                Size = i.ProductVariant.Size.Name,
                Color = i.ProductVariant.Color.Name,
                QuantityRequested = i.QuantityRequested
            }).ToList()
        };
    }
}
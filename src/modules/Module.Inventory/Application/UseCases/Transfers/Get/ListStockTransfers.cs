using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Application.UseCases.Transfers.Get;

public class ListStockTransfers(IInvDbContext context, IUserIntegrationService userIntegrationService, IBranchService branchService)
{
    public async Task<Result<PagedResultDto<ListStockTransferDto>>> Execute(ActorContext ctx, StockTransferQueryDto queryDto)
    {
        var currentBranchId = ctx.BranchIds[0];

        IQueryable<StockTransfer> query = context.StockTransfers.AsQueryable();

        if (queryDto.Status.Count >0)
            query = query.Where(st => queryDto.Status.Contains(st.Status));
        
        if(queryDto.DateFrom.HasValue)
            query = query.Where(st => st.CreatedAt >= queryDto.DateFrom);
        
        if(queryDto.DateTo.HasValue)
            query = query.Where(st => st.CreatedAt <= queryDto.DateTo);

        query = queryDto.Direction switch
        {
            TransferDirection.Inbound => query.Where(st => st.ToBranchId == currentBranchId),
            TransferDirection.Outbound => query.Where(st => st.FromBranchId == currentBranchId),
            _ => query.Where(st => st.ToBranchId == currentBranchId
                                   || st.FromBranchId == currentBranchId)
        };

        var totalCount = await query.CountAsync();

        // Materializar solo lo necesario antes de llamadas externas
        var rawTransfers = await query
            .OrderByDescending(st => st.CreatedAt)
            .ApplyPagination(queryDto)
            .Select(st => new
            {
                st.Id,
                st.Number,
                st.FromBranchId,
                st.ToBranchId,
                st.RequestedByUserId,
                st.Status,
                st.CreatedAt,
                st.ResolvedAt,
                TotalItems = st.Items.Count,
                TotalQuantity = st.Items.Sum(i => i.QuantityRequested)
            })
            .ToListAsync();

        if (rawTransfers.Count == 0)
            return new PagedResultDto<ListStockTransferDto>
            {
                TotalCount = totalCount,
                Items = [],
                Page = queryDto.Page,
                PageSize = queryDto.PageSize
            };

        // Llamadas externas con Ids reales
        var branchIds = rawTransfers
            .SelectMany(t => new[] { t.FromBranchId, t.ToBranchId })
            .Distinct()
            .ToList();

        var userIds = rawTransfers
            .Select(t => t.RequestedByUserId)
            .Distinct()
            .ToList();

        var branchesResult = await branchService.GetBranchesByIds(branchIds);
        if (!branchesResult.IsSuccess)
            return ListStockTransfersErrors.BranchLookupFailed;

        var usersResult = await userIntegrationService.GetUsersByIds(userIds);
        if (!usersResult.IsSuccess)
            return ListStockTransfersErrors.UserLookupFailed;

        var branches = branchesResult.Value.ToDictionary(b => b.Id, b => b.Name);
        var users = usersResult.Value.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var dtos = rawTransfers.Select(t =>
        {
            var isOutbound = t.FromBranchId == currentBranchId;
            var counterpartId = isOutbound ? t.ToBranchId : t.FromBranchId;

            return new ListStockTransferDto
            {
                Id = t.Id,
                Number = t.Number,
                Direction = isOutbound ? TransferDirection.Outbound : TransferDirection.Inbound,
                CounterpartBranchName = branches.GetValueOrDefault(counterpartId) ?? "Unknown",
                RequesterName = users.GetValueOrDefault(t.RequestedByUserId) ?? "Unknown",
                Status = t.Status,
                TotalItems = t.TotalItems,
                TotalQuantity = t.TotalQuantity,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt
            };
        }).ToList();

        return new PagedResultDto<ListStockTransferDto>
        {
            TotalCount = totalCount,
            Items = dtos,
            Page = queryDto.Page,
            PageSize = queryDto.PageSize
        };


    }
}
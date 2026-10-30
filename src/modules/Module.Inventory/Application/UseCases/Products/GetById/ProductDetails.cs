using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Products.GetById;

public class ProductDetails(IInvDbContext context, IBranchService branchService)
{
    public async Task<Result<ProductDetailDto>> Execute(ActorContext ctx, Guid productId)
    {
        var userBranches = ctx.BranchIds;

        var result = await context.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                InternalCode = p.InternalCode,
                Description = p.Description,
                BasePrice = p.BasePrice,
                Gender = p.Gender,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                BrandId = p.BrandId,
                BrandName = p.Brand.Name,
                Variants = p.ProductVariants
                    .OrderBy(pv => pv.Color.Name)
                    .ThenBy(pv => pv.Size.SortOrder)
                    .ThenBy(pv => pv.Sku)
                    .Select(pv => new ProductVarianListDto
                    {
                        Id = pv.Id,
                        Sku = pv.Sku,
                        Description = pv.Description,
                        Size = pv.Size.Name,
                        SizeId = pv.SizeId,
                        Color = pv.Color.Name,
                        ColorId = pv.ColorId,
                        Price = pv.Price,
                        AverageCost = pv.AverageCost,

                        // Latest confirmed reception for this variant. Drafts and reverted receptions are
                        // excluded because they never affected stock. ReceivedAt is rewritten on confirm,
                        // so it reflects the real purchase order; Number breaks same-second ties.
                        LastPurchaseCost = context.StockReceptionItems
                            .Where(i => i.ProductVariantId == pv.Id
                                     && i.StockReception.Status == ReceptionStatus.Confirmed)
                            .OrderByDescending(i => i.StockReception.ReceivedAt)
                            .ThenByDescending(i => i.StockReception.Number)
                            .Select(i => (decimal?)i.UnitCost)
                            .FirstOrDefault(),

                        LastPurchaseAt = context.StockReceptionItems
                            .Where(i => i.ProductVariantId == pv.Id
                                     && i.StockReception.Status == ReceptionStatus.Confirmed)
                            .OrderByDescending(i => i.StockReception.ReceivedAt)
                            .ThenByDescending(i => i.StockReception.Number)
                            .Select(i => (DateTime?)i.StockReception.ReceivedAt)
                            .FirstOrDefault(),

                        BranchStocks = pv.BranchInventories
                            .Where(bi => userBranches.Contains(bi.BranchId))
                            .Select(bi => new BranchStockDto
                            {
                                BranchId = bi.BranchId,
                                Stock = bi.Stock,
                            }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (result is null) return ProductDetailsErrors.ProductNotFound;

        var branchesResult = await branchService.GetBranchesByIds(userBranches.ToList());
        if (!branchesResult.IsSuccess)
            return ProductDetailsErrors.BranchLookupFailed;

        var branchNames = branchesResult.Value.ToDictionary(b => b.Id, b => b.Name);

        foreach (var variant in result.Variants)
        {
            var existing = variant.BranchStocks.ToDictionary(b => b.BranchId);
            variant.BranchStocks = userBranches.Select(bid => existing.TryGetValue(bid, out var bs)
                ? new BranchStockDto { BranchId = bid, BranchName = branchNames.GetValueOrDefault(bid) ?? "Unknown", Stock = bs.Stock }
                : new BranchStockDto { BranchId = bid, BranchName = branchNames.GetValueOrDefault(bid) ?? "Unknown", Stock = 0 }
            ).ToList();
        }

        result.TotalAvailable = result.Variants.Sum(v => v.TotalAvailable);
        return result;
    }
}
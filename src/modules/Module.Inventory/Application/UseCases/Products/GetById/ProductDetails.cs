using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Products.GetById;

public class ProductDetails(IInvDbContext context, IBranchService branchService)
{
    public async Task<Result<ProductDetailDto>> Execute(ActorContext ctx, Guid productId)
    {
        var userBranches = ctx.BranchIds;

        var result = await context.Products.Select(p => new ProductDetailDto
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
            Variants = p.ProductVariants.OrderBy(pv => pv.Color.Name).ThenBy(pv => pv.Size.SortOrder).ThenBy(pv => pv.Sku).Select(pv => new ProductVarianListDto
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
                BranchStocks = pv.BranchInventories
                    .Where(bi => userBranches.Contains(bi.BranchId))
                    .Select(bi => new BranchStockDto
                    {
                        BranchId = bi.BranchId,
                        Stock = bi.Stock,
                    }).ToList()
            }).ToList()
        }).FirstOrDefaultAsync(x => x.Id == productId);

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
using System.Transactions;
using Auth.Contracts.Interfaces;
using Inventory.Contracts.Dtos.Receptions;
using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Receptions;
using Inventory.Data.Persistence;
using Inventory.Infrastructure.CodeGenerator;
using Inventory.UseCases.Products;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Inventory.UseCases.Receptions;

public class CreateReceptionUc(InvDbContext context, ProductUseCases productUseCases, ICurrentUser currentUser)
{
    public async Task<Result<StockReceptionResultDto>> Execute(CreateStockReceptionDto dto)
    {
        var userId = currentUser.UserId;
        var productIds = dto.Items
            .Select(x => x.ProductId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();
        var existingProductsData = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.InternalCode })
            .ToDictionaryAsync(x => x.Id, x => x.InternalCode);
        
        var idsOk = await productUseCases.ValidateProducts.Execute(productIds);
        if (!idsOk.IsSuccess) return new Error(idsOk.Error.Code, idsOk.Error.Message);

        //VALIDATE ALL PRODUCTS VARIANTS REFERENCED
        var productVariants = await GetProductVariants(dto);//all has value and validate
        if (!productVariants.IsSuccess) return productVariants.Error;


        var existingProductsReceptionDto = dto.Items.Where(x => x.ProductId.HasValue).ToList();
        var newProductsReceptionDto = dto.Items.Where(x => !x.ProductId.HasValue).ToList();
        // INIT ATOMIC OPERATION - notes: transaction is useless ok?
        await using var transaction = await context.Database.BeginTransactionAsync();
        var newReception = new StockReception() { BranchId = dto.BranchId, Notes = dto.Notes };
        var stockMovements = new List<StockMovement>();
        try
        {


            foreach (var item in existingProductsReceptionDto)
            {
                var parentInternalCode = existingProductsData[item.ProductId!.Value];

                var variantsToUpdate = item.Variants.Where(v => v.ProductVariantId.HasValue).ToList();
                foreach (var variantDto in variantsToUpdate)
                {
                    var productVariant = productVariants.Value
                        .FirstOrDefault(x => x.Id == variantDto.ProductVariantId!.Value);
                    var stockMovement = StockMovement.CreateReception(dto.BranchId, productVariant!.Id,
                        userId, variantDto.QuantityReceived);

                    newReception.AddExistingVariant(productVariant!.Id, variantDto.QuantityReceived,
                        variantDto.UnitCost);
                    productVariant!.UpdateQuantity(variantDto.QuantityReceived, dto.BranchId);
                    stockMovements.Add(stockMovement);
                }

                var newVariants = item.Variants.Where(v => v.NewVariant != null).ToList();
                foreach (var variantDto in newVariants)
                {
                    var newPv = new ProductVariant
                    {
                        ProductId = item.ProductId!.Value,
                        Description = variantDto.NewVariant!.Description,
                        Size = variantDto.NewVariant.Size,
                        Color = variantDto.NewVariant.Color,
                        Price = variantDto.NewVariant.Price,
                        Sku = CodeGenerator.GenerateVariantSku(parentInternalCode)
                    };
                    var stockMovement = StockMovement.CreateReceptionForNewVariant(dto.BranchId, newPv, userId,
                        variantDto.QuantityReceived, null);

                    stockMovements.Add(stockMovement);
                    newPv.UpdateQuantity(variantDto.QuantityReceived, dto.BranchId);
                    newReception.Items.Add(new StockReceptionItem
                    {
                        ProductVariant = newPv,
                        QuantityReceived = variantDto.QuantityReceived,
                        UnitCost = variantDto.UnitCost
                    });
                }
            }

            foreach (var item in newProductsReceptionDto)
            {
                var productInternalCode = CodeGenerator.GenerateProductCode();
                var newProduct = new Product
                {
                    Name = item.NewProduct!.Name,
                    Description = item.NewProduct.Description,
                    CategoryId = item.NewProduct.CategoryId,
                    BrandId = item.NewProduct.BrandId,
                    BasePrice = item.NewProduct.BasePrice,
                    Gender = item.NewProduct.Gender,
                    InternalCode = productInternalCode
                };


                foreach (var variantDto in item.Variants)
                {
                    var newVariant = new ProductVariant
                    {
                        Description = variantDto.NewVariant!.Description,
                        Price = variantDto.NewVariant.Price,
                        Size = variantDto.NewVariant.Size,
                        Color = variantDto.NewVariant.Color,
                        Sku = CodeGenerator.GenerateVariantSku(productInternalCode)
                    };
                    var stockMovement = StockMovement.CreateReceptionForNewVariant(dto.BranchId, newVariant, userId,
                        variantDto.QuantityReceived);
                    stockMovements.Add(stockMovement);
                    newVariant.UpdateQuantity(variantDto.QuantityReceived, dto.BranchId);
                    newProduct.ProductVariants.Add(newVariant);

                    newReception.Items.Add(new StockReceptionItem
                    {
                        ProductVariant = newVariant,
                        QuantityReceived = variantDto.QuantityReceived,
                        UnitCost = variantDto.UnitCost
                    });
                }

                context.Products.Add(newProduct);
            }

            context.StockReceptions.Add(newReception);
            context.StockMovements.AddRange(stockMovements);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync(); // primera vez
            throw; // relanza la excepción
        }

        var result = await context.StockReceptions
            .Where(r => r.Id == newReception.Id)
            .Select(r => new StockReceptionResultDto
            {
                Id = r.Id,
                BranchId = r.BranchId,
                ReceivedAt = r.ReceivedAt,
                Notes = r.Notes,
                Items = r.Items.Select(i => new StockReceptionItemResultDto
                {
                    ProductVariantId = i.ProductVariantId,
                    // Aquí el JOIN se hace automáticamente en SQL
                    ProductName = i.ProductVariant.Product.Name,
                    VariantDescription = i.ProductVariant.Description,
                    QuantityReceived = i.QuantityReceived,
                    UnitCost = i.UnitCost
                }).ToList()
            }).FirstOrDefaultAsync();
        if (result == null)
            throw new Exception($"La recepción {newReception.Id} se guardó, pero no pudo ser consultada para el resultado.");

        return result;
    }
    private async Task<Result<List<ProductVariant>>> GetProductVariants(CreateStockReceptionDto createStockReceptionDto)
    {
        var variantsIds = createStockReceptionDto.Items.SelectMany(pv => pv.Variants).Where(v => v.ProductVariantId.HasValue).Select(x => x.ProductVariantId!.Value).ToList();
        var productVariants = await context.ProductVariants
            .Include(x => x.BranchInventories)
            .Where(x => variantsIds.Contains(x.Id)).ToListAsync();

        var foundIds = productVariants.Select(x => x.Id).ToList();
        var missingIds = variantsIds.Except(foundIds).ToList();

        if (missingIds.Count != 0)
            return new Error("NOT_FOUND", $"the next Ids of VariantIds were Not founded: {missingIds}");
        return productVariants;
    }

}
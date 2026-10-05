
using Module.Inventory.Application.UseCases.ProductVariants.Create;
using Module.Inventory.Application.UseCases.ProductVariants.Delete;
using Module.Inventory.Application.UseCases.ProductVariants.GetById;
using Module.Inventory.Application.UseCases.ProductVariants.GetBySku;
using Module.Inventory.Application.UseCases.ProductVariants.PatchStock;
using Module.Inventory.Application.UseCases.ProductVariants.Search;
using Module.Inventory.Application.UseCases.ProductVariants.Update;

namespace Module.Inventory.Application.UseCases.ProductVariants;

public record ProductVariantUseCases(
    GetProductVariantDetails GetProductVariantDetails
    ,UpdateProductVariant UpdateProductVariant
    , CorrectProductVariantStock CorrectProductVariantStock
    ,GetProductVariantByCode  GetProductVariantByCode
    ,SearchProductVariants SearchProductVariants
    ,CreateProductVariantUc CreateProductVariantUc
    ,DeleteProductVariantUc DeleteProductVariantUc);
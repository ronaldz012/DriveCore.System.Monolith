using Common.Utilities;

namespace Module.Inventory.Application.UseCases.ProductVariants.PatchStock;

public static class CorrectProductVariantStockErrors
{
    public static readonly Error VariantNotFound = new(ErrorCode.NotFound, "Product variant not found");
    public static readonly Error StockCorrectionFailed = new(ErrorCode.ValidationError, "Stock correction failed");
}

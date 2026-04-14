using System.ComponentModel.DataAnnotations;

namespace Inventory.Contracts.Dtos.Receptions;

public class CreateStockReceptionDto : IValidatableObject
{
    public int BranchId { get; set; }
    public string? Notes { get; set; }
    public List<CreateStockReceptionItemDto> Items { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Items.Any())
            yield return new ValidationResult("At least one item is required", [nameof(Items)]);

        // Productos nuevos deben tener NewProduct y todas sus variantes con NewVariant
        var newProducts = Items.Where(x => x.ProductId == null).ToList();

        if (newProducts.Any(x => x.NewProduct == null))
            yield return new ValidationResult(
                "NewProduct is required when ProductId is not provided",
                [nameof(Items)]);

        if (newProducts.Any(x => x.Variants.Any(v => v.NewVariant == null)))
            yield return new ValidationResult(
                "All variants must have NewVariant when creating a new product",
                [nameof(Items)]);

        // Productos existentes deben tener al menos una variante
        var existingProducts = Items.Where(x => x.ProductId.HasValue).ToList();

        if (existingProducts.Any(x => !x.Variants.Any()))
            yield return new ValidationResult(
                "Each existing product must have at least one variant",
                [nameof(Items)]);
    }
}
public class CreateStockReceptionItemDto
{
    // Si el producto ya existe
    public int? ProductId { get; set; }

    // Si es producto nuevo
    public NewProductDto? NewProduct { get; set; }

    // Variantes — siempre una lista
    public List<NewStockReceptionVariantDto> Variants { get; set; } = new();
}

public class NewStockReceptionVariantDto
{
    // Si la variante ya existe
    public int? ProductVariantId { get; set; }

    // Si es variante nueva
    public NewProductVariantDto? NewVariant { get; set; }

    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}
public class NewProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public decimal BasePrice { get; set; }
    public int UnitMeasurementSin { get; set; }
    public string EconomicActivity { get; set; } = string.Empty;
    public int ProductCodeSin { get; set; }
}

public class NewProductVariantDto
{
    public int? ProductId { get; set; } // Si el producto ya existe pero la variante es nueva
    public string Description { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
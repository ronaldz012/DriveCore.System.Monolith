namespace Inventory.Contracts.Dtos.Receptions;

public class CreateStockReceptionDto
{
    public int BranchId { get; set; }
    public string? Notes { get; set; }
    public List<CreateStockReceptionItemDto> Items { get; set; } = new();
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
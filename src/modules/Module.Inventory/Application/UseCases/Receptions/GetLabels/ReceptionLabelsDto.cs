using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Receptions.GetLabels;

public class ReceptionLabelsDto
{
    public Guid ReceptionId { get; set; }
    public int Number { get; set; }
    public DateTime ReceptionDate { get; set; }
    public List<ReceptionLabelItemDto> Items { get; set; } = new();
}
public class ReceptionLabelItemDto
{
    public Guid VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public decimal Price { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}


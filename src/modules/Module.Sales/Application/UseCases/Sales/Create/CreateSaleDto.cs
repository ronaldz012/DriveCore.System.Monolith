using System.ComponentModel.DataAnnotations;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Create;

public class CreateSaleDto : IValidatableObject
{
    public PaymentMethod PaymentMethod { get; set; }
    public int? InvoiceNumber { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? TransactionCode { get; set; }
    public string? Notes { get; set; }

    // Lista de productos a vender
    public List<CreateSaleItemDto> Items { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Duplicates only apply to catalog variants; free items are always distinct.
        var duplicate = Items
            .Where(x => x.ProductVariantId.HasValue)
            .GroupBy(x => x.ProductVariantId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            yield return new ValidationResult("Duplicate variant items in request", [nameof(Items)]);

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];

            if (item.ProductVariantId == Guid.Empty)
            {
                yield return new ValidationResult($"Items[{i}].ProductVariantId is invalid", [nameof(Items)]);
                continue;
            }

            if (item.Quantity < 1)
                yield return new ValidationResult($"Items[{i}].Quantity must be greater than 0", [nameof(Items)]);

            if (item.ProductVariantId is null)
            {
                // Free item: description and price are mandatory, no catalog data involved.
                if (string.IsNullOrWhiteSpace(item.Description))
                    yield return new ValidationResult($"Items[{i}].Description is required for free items", [nameof(Items)]);
                else if (item.Description.Trim().Length > 120)
                    yield return new ValidationResult($"Items[{i}].Description must be 120 characters or less", [nameof(Items)]);

                if (item.UnitPrice is null || item.UnitPrice < 0.5m)
                    yield return new ValidationResult($"Items[{i}].UnitPrice is required for free items and must be at least 0.5", [nameof(Items)]);
            }
            else if (!string.IsNullOrWhiteSpace(item.Description) || item.UnitPrice.HasValue)
            {
                // Variant items take description and price from the catalog.
                yield return new ValidationResult($"Items[{i}] cannot mix ProductVariantId with Description/UnitPrice", [nameof(Items)]);
            }
        }
    }
}

public class CreateSaleItemDto
{
    // Either a registered variant or a free item with Description + UnitPrice.
    public Guid? ProductVariantId { get; set; }
    public string? Description { get; set; }
    public decimal? UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
}

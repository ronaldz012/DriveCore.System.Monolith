using System.ComponentModel.DataAnnotations;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Edit;

public class AddDraftItemsDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<DraftItemDto> Items { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // UnitCost >= 0: drafts may hold 0 cost; confirming enforces the minimum later.
        foreach (var item in Items)
        {
            if (item.UnitCost < 0)
                yield return new ValidationResult("UnitCost cannot be negative", [nameof(Items)]);
        }

        var duplicate = Items
            .GroupBy(x => x.ProductVariantId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            yield return new ValidationResult("Duplicate variant items in request", [nameof(Items)]);
    }
}

public class UpdateDraftDto : IValidatableObject
{
    [Required]
    public Guid ProviderId { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<DraftItemDto> Items { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var item in Items)
        {
            if (item.UnitCost < 0)
                yield return new ValidationResult("UnitCost cannot be negative", [nameof(Items)]);
        }

        var duplicate = Items
            .GroupBy(x => x.ProductVariantId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            yield return new ValidationResult("Duplicate variant items in request", [nameof(Items)]);
    }
}

public class DraftItemDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, 999999999, ErrorMessage = "QuantityReceived must be greater than 0")]
    public int QuantityReceived { get; set; }

    public decimal UnitCost { get; set; }
}

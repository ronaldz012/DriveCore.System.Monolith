using System.ComponentModel.DataAnnotations;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Update;

public class UpdateProductDto : IValidatableObject
{
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }

    [EnumDataType(typeof(Gender), ErrorMessage = "Género no válido")]
    public Gender? Gender { get; set; }
    public List<VariantPriceItem> VariantPrices { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicate = VariantPrices
            .GroupBy(x => x.VariantId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            yield return new ValidationResult("Duplicate variants in request", [nameof(VariantPrices)]);
    }
}

public class VariantPriceItem
{
    [Required]
    public Guid VariantId { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Price { get; set; }
}

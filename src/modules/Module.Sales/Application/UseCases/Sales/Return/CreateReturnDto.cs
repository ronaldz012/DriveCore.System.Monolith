using System.ComponentModel.DataAnnotations;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return;

public class CreateReturnDto : IValidatableObject
{
    [Required]
    public Guid OriginalSaleId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<CreateReturnItemDto> Items { get; set; } = new();

    // How the refund is handed back, chosen by the seller. Amounts are positive
    // ("Bs 60 by QR"); the backend negates them when storing. Their sum must equal
    // the refund total (validated in the use case, which resolves the total).
    [Required]
    [MinLength(1, ErrorMessage = "At least one payment is required")]
    public List<CreateReturnPaymentDto> Payments { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicate = Items
            .GroupBy(x => x.OriginalSaleItemId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            yield return new ValidationResult("Duplicate items in request", [nameof(Items)]);

        foreach (var payment in Payments)
        {
            if (payment.Amount <= 0)
                yield return new ValidationResult("Payment amounts must be greater than 0", [nameof(Payments)]);
        }

        // One tender per payment method per return (backed by a unique DB index).
        var duplicateMethod = Payments
            .GroupBy(p => p.Method)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateMethod != null)
            yield return new ValidationResult("Duplicate payment method in request", [nameof(Payments)]);
    }
}

public class CreateReturnItemDto
{
    [Required]
    public Guid OriginalSaleItemId { get; set; }

    [Range(1, 99999, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}

public class CreateReturnPaymentDto
{
    [Required]
    public PaymentMethod Method { get; set; }

    [Range(0.01, 999999999999, ErrorMessage = "Payment amount must be greater than 0")]
    public decimal Amount { get; set; }
}

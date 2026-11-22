using Common.Domain;

namespace Module.Sales.Domain;

/// <summary>
/// One payment tender of a sale. Sales may be paid with several methods
/// (e.g. half QR, half cash); returns store negative amounts.
/// One line per (Sale, Method) — enforced by a unique index.
/// Sale.PaymentMethod keeps the dominant tender for backward compatibility.
/// </summary>
public class SalePayment : IMustHaveTenant, ICreatedAt, ICreatedBy, IUpdatedAt, IUpdatedBy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }

    public Sale Sale { get; set; } = null!;
}

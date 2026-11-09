using Common.Utilities;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Get;

public class SaleListDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public string FirstItemDisplayName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int TotalDistinctItems { get; set; }
    public SaleType Type { get; set; }
    public Guid? OriginalSaleId { get; set; }
    public int? OriginalSaleNumber { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DocumentType DocumentType { get; set; } 
    public int? InvoiceNumber { get; set; }
    public string? TransactionCode { get; set; }
    public bool HasReturn { get; set; }
    public SaleReturnStatus ReturnStatus { get; set; }
    public decimal ReturnedAmount { get; set; }
}

public class SalesQueryDto : PaginationQueryDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public SaleType? Type { get; set; }

    // Filter by the seller who registered the sale.
    public Guid? SoldById { get; set; }

    // Legacy flag kept for backward compatibility; maps to ReturnStatus (true = Partial + Full).
    public bool? HasReturn { get; set; }

    // Takes precedence over HasReturn when both are sent.
    public SaleReturnStatus? ReturnStatus { get; set; }

    public string? Filter { get; set; }
}

using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.GetById;

public class SaleDetailDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public Guid SoldById { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public SaleType Type { get; set; }
    public DocumentType DocumentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionCode { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public int? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? OriginalSaleId { get; set; }

    public List<SaleItemDetailDto> Items { get; set; } = [];
    public List<SaleRefundDto> Returns { get; set; } = [];
}

public class SaleRefundDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SaleItemDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public string ProductDisplayName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public int ReturnedQuantity { get; set; }
}

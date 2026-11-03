using Common.Domain;

namespace Module.Sales.Domain;

public class SaleItem : IMustHaveTenant, ICreatedAt, ICreatedBy, IUpdatedAt, IUpdatedBy, ISoftDelete
{
    public Guid Id { get; set; } 
    public Guid TenantId { get; set; }
    public Guid SaleId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductDisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeletedByName { get; set; }

    public Guid? OriginalSaleItemId { get; set; }

    public Sale Sale { get; set; } = null!;
    public SaleItem? OriginalSaleItem { get; set; }
    public ICollection<SaleItem> ChildReturns { get; set; } = new List<SaleItem>();
}
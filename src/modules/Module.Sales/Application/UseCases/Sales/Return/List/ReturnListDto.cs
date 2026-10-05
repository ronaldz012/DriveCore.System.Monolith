using Common.Utilities;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return.List;

public class ReturnListDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public string FirstItemDisplayName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? OriginalSaleId { get; set; }
    public string? Notes { get; set; }
}

public class ReturnsQueryDto : PaginationQueryDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

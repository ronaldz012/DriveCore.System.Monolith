using Common.Utilities;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Get;

public class StockReceptionListDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public ReceptionStatus Status { get; set; }
    public string? Notes { get; set; }
    public int TotalItems { get; set; }
    public int ProductVariantsCount { get; set; }
    public decimal TotalCost { get; set; }
    public List<string> BrandNames { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];

}

public class ReceptionQueryDto : PaginationQueryDto
{
    public string? Filter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public ReceptionStatus? Status { get; set; }
    public Guid? BrandId { get; set; }
}
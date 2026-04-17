using Inventory.Data.Entities.Receptions;
using Shared.Extensions;

namespace Inventory.Contracts.Dtos.Receptions;

public class StockReceptionListDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalCost { get; set; }
    public List<string> Brands { get; set; } = [];
    public List<string> Categories { get; set; } = [];
}

public class ReceptionQueryDto : GenericPaginationQueryDto
{
    public int BranchId { get; set; }
}
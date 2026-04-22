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
    public Dictionary<string, List<string>> Types { get; set; } = [];
}

public class ReceptionQueryDto : GenericPaginationQueryDto
{

}
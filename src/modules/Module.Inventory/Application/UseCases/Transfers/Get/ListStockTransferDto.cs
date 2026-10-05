using Common.Utilities;
using Module.Inventory.Domain.Transfers;
namespace Module.Inventory.Application.UseCases.Transfers.Get;

public class ListStockTransferDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public TransferDirection Direction { get; set; }
    public string CounterpartBranchName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public TransferStatus Status { get; set; }
    public int TotalItems { get; set; }      // count de items distintos
    public int TotalQuantity { get; set; }   // suma de QuantityRequested

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class StockTransferQueryDto : PaginationQueryDto
{
    public List<TransferStatus> Status { get; set; } = [];
    public TransferDirection? Direction { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

}
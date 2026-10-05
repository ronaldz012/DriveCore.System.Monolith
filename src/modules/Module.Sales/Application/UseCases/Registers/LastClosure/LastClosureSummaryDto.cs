namespace Module.Sales.Application.UseCases.Registers.LastClosure;

public class LastClosureSummaryDto
{
    public bool HasData { get; set; }
    public Guid? ClosureId { get; set; }
    public int? Number { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal TotalSales { get; set; }
    public int SalesCount { get; set; }
    public int ItemsSold { get; set; }
    public int RestockCount { get; set; }
}
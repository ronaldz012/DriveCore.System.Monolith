namespace Module.Sales.Application.UseCases.Registers.List;

public class ClosureListDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public string? ClosedByName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal SystemSalesAmount { get; set; }
    public decimal RealCountedAmount { get; set; }
    public decimal Difference { get; set; }
}

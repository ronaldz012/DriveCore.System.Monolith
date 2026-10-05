namespace Module.Sales.Application.UseCases.Registers.Close;

public class CloseCashRegisterResponseDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CashSalesTotal { get; set; }
    public decimal OutflowsTotal { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal RealCountedAmount { get; set; }
    public decimal Difference { get; set; }
    public DateTime ClosedAt { get; set; }
    public string OpenByName { get; set; } = string.Empty;
    public string? CloseByName { get; set; }
}

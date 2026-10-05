namespace Module.Sales.Application.UseCases.Registers.TodaySales;

public class TodaySalesDto
{
    public bool IsOpen { get; set; }
    public Guid? ClosureId { get; set; }
    public int? Number { get; set; }
    public decimal? OpeningBalance { get; set; }
    public DateTime? OpeningAt { get; set; }
    public string OpenedByName { get; set; } = string.Empty;

    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public decimal CashAmount { get; set; }
    public decimal QrCodeAmount { get; set; }
    public int TicketCount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageTicket { get; set; }
}
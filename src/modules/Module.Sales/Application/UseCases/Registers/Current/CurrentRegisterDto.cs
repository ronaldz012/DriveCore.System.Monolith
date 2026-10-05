namespace Module.Sales.Application.UseCases.Registers.Current;

public class CurrentRegisterDto
{
    public bool IsOpen { get; set; }
    public Guid? ClosureId { get; set; }
    public int? Number { get; set; }
    public decimal? OpeningBalance { get; set; }
    public DateTime? OpenedAt { get; set; }
    public string OpenByName { get; set; } = string.Empty;
}

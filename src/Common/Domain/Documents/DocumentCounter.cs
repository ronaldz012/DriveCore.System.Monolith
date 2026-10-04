namespace Common.Domain.Documents;

public class DocumentCounter
{
    public Guid TenantId { get; set; }
    public string CounterKey { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}
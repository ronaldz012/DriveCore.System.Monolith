namespace Inventory.Data.Entities.Shared.Base;

public class Params
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime DeletedAt { get; set; }
    public int? CreatedById { get; set; }
    public int? UpdatedById { get; set; }
    public int? DeletedById { get; set; }

    public Params()
    {
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.MinValue;
        DeletedAt = DateTime.MinValue;
    }
}

using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Organization;

public class PointOfSsale:Params
{
    public int id { get; set; }
    public int SucuralId { get; set; }
    public int ExternalId { get; set; }
    public int SiatCode { get; set; }
}

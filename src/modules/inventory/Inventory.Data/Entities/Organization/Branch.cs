using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Organization;

public class Branch : Params
{
    public int Id { get; set; }
    public int ExternalId { get; set; } // Id del AuthService
    public int BranchSiatCode { get; set; } //Codigo de Sucursal
    public int PointOfSaleSiatCode { get; set; } = 0;//Cuando se factura con la sucural en general y no con un punto de venta se envia 0
    
    public ICollection<PointOfSsale> pointOfSsales { get; set; } = new List<PointOfSsale>();

}

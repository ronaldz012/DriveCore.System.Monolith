using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Products; 

public class Product:Params
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int BrandId { get; set; }

    public int UnitMeasurementSin { get; set; } // unidad de medida siat
    public string EconomicActivity { get; set; } = string.Empty; // codigo actividad economica siat
    public int ProductCodeSin { get; set; } // codigo producto SIN siat

    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public Category Category { get; set; } = default!;
    public Brand Brand { get; set; } = null!;
}

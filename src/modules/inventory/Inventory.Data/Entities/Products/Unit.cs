using Inventory.Data.Entities.Shared.Base;
using Inventory.Data.Entities.Shared.Enums;

namespace Inventory.Data.Entities.Products;

public class Unit : Params
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Nombre de la unidad de medida
    public string Abbreviation { get; set; } = string.Empty; // Abreviatura de la unidad de medida
    public string Description { get; set; } = string.Empty; // Descripción de la unidad de medida
    public UnitType UnitType { get; set; } // Tipo de unidad de medida (por ejemplo,Unidas, peso, volumen, etc.)
    public DateTime? DeletedAt { get; set; } = null;
}

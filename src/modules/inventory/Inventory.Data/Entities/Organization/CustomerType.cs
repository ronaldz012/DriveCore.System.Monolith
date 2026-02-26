using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Organization;

public class CustomerType : Params
{
    public int Id { get; set; } // Identificador único del tipo de cliente
    public string Name { get; set; } = string.Empty; // Nombre del tipo de cliente
    public string Description { get; set; } = string.Empty; // Descripción del tipo de cliente
}

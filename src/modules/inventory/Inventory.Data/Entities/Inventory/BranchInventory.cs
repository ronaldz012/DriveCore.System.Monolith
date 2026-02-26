using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Inventory;

public class BranchInventory: Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } // Id de la sucursal
    public int ProductVariantId { get; set; } // Id de la variante del producto
    public decimal CurrentStock { get; set; } // Stock actual del producto en la sucursal
    public decimal ReservedStock { get; set; } // Stock reservado del producto en la sucursal
    public decimal MinStock { get; set; } // Stock minimo del producto en la sucursal
    public decimal MaxStock { get; set; } // Stock maximo del producto en la sucursal


}

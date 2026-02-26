using Inventory.Data.Entities.Shared.Base;
using Inventory.Data.Entities.Shared.Enums;

namespace Inventory.Data.Entities.Inventory;

public class StockMovement : Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } // Id de la sucursal
    public int ProductVariantId { get; set; } // Id de la variante del producto
    public decimal Quantity { get; set; } // Cantidad de stock movida
    public string BatchNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty; // Notas adicionales sobre el movimiento de stock
    public DateTime ExpirationDate { get; set; } // Fecha de expiración del lote
    public MovementType MovementType { get; set; } // Tipo de movimiento (Ingreso, Egreso, Ajuste, etc.)
}

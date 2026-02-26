namespace Inventory.Data.Entities.Sales;

public class SaleItem
{
    public int Id { get; set; } // Id del item de venta
    public int SaleId { get; set; } // Id de la venta a la que pertenece el item
    public int ProductVariantId { get; set; } // Id de la variante del producto
    public decimal Quantity { get; set; } // Cantidad del producto vendido
    public decimal UnitPrice { get; set; } // Precio unitario del producto
    public decimal UnitCost { get; set; } // Descuento aplicado al item
    public decimal Discount { get; set; } // Descuento aplicado al item
    public decimal TotalPrice { get; set; } // Precio total del item (Quantity * Price)
}

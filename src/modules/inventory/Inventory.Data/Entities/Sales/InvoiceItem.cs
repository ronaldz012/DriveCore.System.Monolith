using Inventory.Data.Entities.Shared.Enums;

namespace Inventory.Data.Entities.Sales;

public class InvoiceItem
{
    public int Id { get; set; } // ID del item de la factura
    public int ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public float Total { get; set; }
    public float Price { get; set; }
    public float Discount { get; set; }
    public int MeasurementUnit { get; set; } // ID de la unidad de medida
    public string ActivityCode { get; set; } = string.Empty;// ID de la actividad económica
    public int ServiceProductCode { get; set; } // ID del código de servicio o producto
    public Invoice Invoice { get; set; } = null!; // Relación con la entidad Invoice
    public InvoiceItemType ItemType { get; set; } // Tipo de item (producto o servicio)
}

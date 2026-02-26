namespace Inventory.Data.Entities.Sales;

public class Payment
{
    public int Id { get; set; } // ID del pago
    public int InvoiceId { get; set; } // ID de la factura
    public string Type { get; set; } = string.Empty; // Tipo de pago (Efectivo, Tarjeta, etc.)
    public int TypeId { get; set; } // ID del tipo de pago
    public float Amount { get; set; } // Monto total pagado
    public string VoucherNumber { get; set; } = string.Empty; // Numero de comprobante

    public Invoice Invoice { get; set; } = null!; // Relación con la entidad Invoice
}

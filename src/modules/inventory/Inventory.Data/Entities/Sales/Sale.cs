using Inventory.Data.Entities.Shared.Base;
using Inventory.Data.Entities.Shared.Enums;

namespace Inventory.Data.Entities.Sales;

public class Sale : Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } // Id de la sucursal
    public int CustomerId { get; set; } // Id del cliente

    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; } // Monto del impuesto
    decimal TotalDiscount { get; set; } // Monto del descuento total aplicado a la venta
    PaymentMethod PaymentMethod { get; set; } // Metodo de pago utilizado en la venta

}

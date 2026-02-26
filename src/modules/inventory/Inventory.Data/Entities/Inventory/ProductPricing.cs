using Inventory.Data.Entities.Organization;
using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Inventory;

/// <summary>
/// Existe un Margen diferente para cada sucursal y cada Tipo de cliente
/// </summary>
public class ProductPricing:Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } // Id de la sucursal
    public int ProductVariantId { get; set; } // Id de la variante del producto
    public int CustomerTypeId { get; set; } 
    public decimal SellingPrice { get; set; }  // Precio de venta del producto segun cliente y otros
    public decimal MarginPrice { get; set; } // Precio de margen del producto segun cliente y otros
    public ProductVariant ProductVariant { get; set; } = default!; // Producto al que pertenece el precio
    public Branch Branch { get; set; } = default!; // Sucursal a la que pertenece el precio
    public CustomerType customerType { get; set; } = default!; // Tipo de cliente al que pertenece el precio

}

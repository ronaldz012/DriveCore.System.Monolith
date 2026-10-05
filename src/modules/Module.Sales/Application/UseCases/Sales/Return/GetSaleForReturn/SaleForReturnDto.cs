using System.ComponentModel.DataAnnotations;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;

public class SaleForReturnDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public SaleType Type { get; set; }
    public List<ReturnableItemDto> Items { get; set; } = new();
}

public class ReturnableItemDto
{
    public Guid SaleItemId { get; set; }
    public string ProductDisplayName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReturnableQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Common.Utilities;

namespace Module.Sales.Application.UseCases.Sales.Search;

public class SkuSearchQueryDto : PaginationQueryDto
{
    [Required]
    public string Sku { get; set; } = string.Empty;

    [Range(1, 90)]
    public int Days { get; set; } = 7;
}

public class SkuSearchResponseDto
{
    public string SearchedSku { get; set; } = string.Empty;
    public string SearchedDisplayName { get; set; } = string.Empty;
    public PagedResultDto<SaleSkuSearchDto> Sales { get; set; } = new();
}

public class SaleSkuSearchDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int TotalUnitsSold { get; set; }
    public MatchedItemDto MatchedItem { get; set; } = new();
}

public class MatchedItemDto
{
    public Guid SaleItemId { get; set; } 
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

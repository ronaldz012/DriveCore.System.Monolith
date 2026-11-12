using System.ComponentModel.DataAnnotations;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Stats;

public class SalesStatsQueryDto : IValidatableObject
{
    [Required]
    public DateTime? DateFrom { get; set; }

    [Required]
    public DateTime? DateTo { get; set; }

    // Optional seller filter. Scope rules are enforced by the controller.
    public Guid? SoldById { get; set; }

    // Minutes added to UTC to bucket by the branch local day (e.g. -240 for UTC-4).
    [Range(-840, 840, ErrorMessage = "tzOffsetMinutes must be between -840 and 840")]
    public int TzOffsetMinutes { get; set; } = -240;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Presence is covered by [Required]; only cross-field rules live here.
        if (DateFrom.HasValue && DateTo.HasValue && DateTo.Value < DateFrom.Value)
            yield return new ValidationResult(
                "dateTo must be greater than or equal to dateFrom",
                [nameof(DateTo)]);
    }
}

public class SalesStatsDto
{
    public SalesStatsTotalsDto Totals { get; set; } = new();
    public List<SalesStatsByDayDto> ByDay { get; set; } = [];
    public List<SalesStatsByPaymentMethodDto> ByPaymentMethod { get; set; } = [];
    public List<SalesStatsBySellerDto> BySeller { get; set; } = [];
    public List<SalesStatsTopProductDto> TopProducts { get; set; } = [];
}

public class SalesStatsTotalsDto
{
    public int SalesCount { get; set; }
    public int ReturnsCount { get; set; }
    public decimal GrossSalesAmount { get; set; }
    public decimal ReturnsAmount { get; set; }
    public decimal NetSalesAmount { get; set; }
    public int UnitsSold { get; set; }
    public int UnitsReturned { get; set; }
    public decimal AvgTicket { get; set; }
    public decimal DiscountAmount { get; set; }

    // Sales split by item kind (free items have no catalog variant).
    public decimal FreeSalesAmount { get; set; }
    public int FreeSalesCount { get; set; }

    // Only filled when the actor has the sales:margin permission.
    public decimal? GrossMargin { get; set; }
    public decimal? MarginPct { get; set; }
    public decimal? MarginCoveragePct { get; set; }
}

public class SalesStatsByDayDto
{
    public DateOnly Date { get; set; }
    public int SalesCount { get; set; }
    public decimal NetSalesAmount { get; set; }
}

public class SalesStatsByPaymentMethodDto
{
    public PaymentMethod Method { get; set; }
    public int SalesCount { get; set; }
    public decimal Amount { get; set; }
}

public class SalesStatsBySellerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal NetSalesAmount { get; set; }
    public decimal AvgTicket { get; set; }
}

public class SalesStatsTopProductDto
{
    public Guid ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Units { get; set; }
    public decimal NetSalesAmount { get; set; }
    public decimal? GrossMargin { get; set; } // null when the actor cannot see margins or there is no cost
}

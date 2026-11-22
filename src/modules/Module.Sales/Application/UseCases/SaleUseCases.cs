using Module.Sales.Application.UseCases.Sales.Create;
using Module.Sales.Application.UseCases.Sales.Get;
using Module.Sales.Application.UseCases.Sales.GetById;
using Module.Sales.Application.UseCases.Sales.Return;
using Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;
using Module.Sales.Application.UseCases.Sales.Stats;

namespace Module.Sales.Application.UseCases;

public record SaleUseCases(
    CreateSale CreateSale,
    GetListSales GetListSales,
    GetSaleDetail GetSaleDetail,
    ListSellers ListSellers,
    GetSalesStats GetSalesStats,
    CreateReturn CreateReturn,
    GetSaleForReturn GetSaleForReturn);
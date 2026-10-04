using Common.Contracts.sales;
using Microsoft.Extensions.DependencyInjection;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases;
using Module.Sales.Application.UseCases.Movements.Create;
using Module.Sales.Application.UseCases.Movements.Delete;
using Module.Sales.Application.UseCases.Movements.List;
using Module.Sales.Application.UseCases.Movements.Update;
using Module.Sales.Application.UseCases.Registers.Close;
using Module.Sales.Application.UseCases.Registers.Current;
using Module.Sales.Application.UseCases.Registers.GetById;
using Module.Sales.Application.UseCases.Registers.LastClosure;
using Module.Sales.Application.UseCases.Registers.List;
using Module.Sales.Application.UseCases.Registers.Open;
using Module.Sales.Application.UseCases.Registers.TodaySales;
using Module.Sales.Application.UseCases.Sales.Create;
using Module.Sales.Application.UseCases.Sales.Get;
using Module.Sales.Application.UseCases.Sales.GetById;
using Module.Sales.Application.UseCases.Sales.Return;
using Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;
using Module.Sales.Application.UseCases.Sales.Return.List;
using Module.Sales.Application.UseCases.Sales.Search;
using Module.Sales.Infrastructure;
using Module.Sales.Infrastructure.Services;

namespace Module.Sales;

public static class SalesDependencyInjection
{

        public static IServiceCollection AddSales(this IServiceCollection services)
        {
                services.AddScoped<SaleUseCases>()
                        .AddScoped<CreateSale>()
                        .AddScoped<GetListSales>()
                        .AddScoped<GetSaleDetail>()
                        .AddScoped<CreateReturn>()
                        .AddScoped<ListReturns>()
                        .AddScoped<SearchSalesBySku>()
                        .AddScoped<GetSaleForReturn>()
                        .AddScoped<RegisterUseCases>()
                        .AddScoped<OpenCashRegister>()
                        .AddScoped<CloseCashRegister>()
                        .AddScoped<GetCurrentRegister>()
                        .AddScoped<ListClosures>()
                        .AddScoped<GetClosureDetail>()
                        .AddScoped<GetTodaySales>()
                        .AddScoped<GetLastClosureSummary>()
                        .AddScoped<MovementUseCases>()
                        .AddScoped<CreateMovement>()
                        .AddScoped<ListMovements>()
                        .AddScoped<UpdateMovement>()
                        .AddScoped<DeleteMovement>();

                services.AddScoped<ISalesIntegrationService, SalesIntegrationService>();
                services.AddScoped<ISalesNumberGenerator, SalesNumberGenerator>();

                return services;
        }
}
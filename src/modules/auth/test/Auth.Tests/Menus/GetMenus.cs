using System;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using Auth.UseCases.mapper;
using Auth.UseCases.Menus;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Menus;

public class GetMenus
{
    private AuthDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Asumiendo que AuthDbContext tiene un constructor que acepta DbContextOptions<AuthDbContext>
        return new AuthDbContext(options);
    }
    private IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(MappingConfig).Assembly);
        return new Mapper(config);
    }

    [Fact]
    public async Task GetAllMenus_ShouldReturnMenus_WhenMenusExist()
    {
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        // Seed data
        dbContext.Menus.AddRange(new[]
        {
            new Auth.Data.Entities.Menu { Id = 1, Name = "Menu 1", ModuleId = 1 },
            new Auth.Data.Entities.Menu { Id = 2, Name = "Menu 2", ModuleId = 1 },
            new Auth.Data.Entities.Menu { Id = 3, Name = "Menu 3", ModuleId = 2 }
        });
        await dbContext.SaveChangesAsync();

        var getAllMenus = new GetAllMenus(dbContext);
        var queryDto = new MenuQueryDto
        {
            ModuleId = 1,
            Page = 1,
            PageSize = 10
        };

        var result = await getAllMenus.Execute(queryDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count());
    }
    [Fact]
    public async Task GetMenu_ShouldReturnMenu_WhenMenuExists()
    {
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        // Seed data
        var menu = new Auth.Data.Entities.Menu { Id = 1, Name = "Menu 1", ModuleId = 1 };
        dbContext.Menus.Add(menu);
        await dbContext.SaveChangesAsync();

        var getMenu = new GetMenu(dbContext, mapper);

        var result = await getMenu.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Menu 1", result.Value!.Name);
    }
    [Fact]
    public async Task GetMenu_ShouldReturnNotFoundError_WhenMenuDoesNotExist()
    {
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var getMenu = new GetMenu(dbContext, mapper);

        var result = await getMenu.Execute(999); // ID que no existe

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

}

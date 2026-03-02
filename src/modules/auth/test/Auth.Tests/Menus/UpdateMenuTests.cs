using System;
using System.Net.Http.Headers;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.UseCases.mapper;
using Auth.UseCases.Menus;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Menus;

public class UpdateMenuTests
{
    private AuthDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Asumiendo que AuthDbContext tiene un constructor que acepta DbContextOptions<AuthDbContext>
        return new AuthDbContext(options);
    }
    private MapsterMapper.IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(MappingConfig).Assembly);
        return new Mapper(config);
    }

    [Fact]
    public async Task UpdateMenu_ShouldUpdateMenu_WhenValidRequest()
    {
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        var menuToUpdate = new Menu
        {
            Id = 1,
            Name = "Original Menu",
            Route = "/original-route",
            ModuleId = 1,
            ParentMenuId = 0,
            Icon = "original-icon",
            Order = 1
        };
        dbContext.Menus.Add(menuToUpdate);
        await dbContext.SaveChangesAsync();
        //// SETUP
        var updateMenu = new UpdateMenu(dbContext, mapper);
        var dto = new UpdateMenuDto
        {
            Id = menuToUpdate.Id,
            Name = "Updated Menu",
            Route = "/updated-route",
            ModuleId = menuToUpdate.ModuleId,
            ParentMenuId = menuToUpdate.ParentMenuId,
            Icon = "updated-icon",
            Order = 2
        };

        // ACT
        var result = await updateMenu.Execute(dto);
        // ASSERT
        Assert.True(result.IsSuccess);
        var updatedMenu = await dbContext.Menus.FindAsync(menuToUpdate.Id);
        Assert.NotNull(updatedMenu);
        Assert.Equal("Updated Menu", updatedMenu.Name);
        Assert.Equal("/updated-route", updatedMenu.Route);
        Assert.Equal("updated-icon", updatedMenu.Icon);
        Assert.Equal(2, updatedMenu.Order);
    }

    [Fact]
    public async Task UpdateMenu_ShouldReturnNotFoundError_WhenMenuDoesNotExist()
    {
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        // SETUP
        var updateMenu = new UpdateMenu(dbContext, mapper);
        var dto = new UpdateMenuDto
        {
            Id = 999, // ID que no existe
            Name = "Non-existent Menu",
            Route = "/non-existent-route",
            ModuleId = 1,
            ParentMenuId = 0,
            Icon = "non-existent-icon",
            Order = 1
        };

        // ACT
        var result = await updateMenu.Execute(dto);

        // ASSERT
        Assert.True(result.Error != null);
        Assert.Equal("NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task UpdateMenu_ShouldReturnDuplicateError_WhenMenuNameExistsInModule()
    {
        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateInMemoryDbContext(dbName);
        var mapper = CreateMapper();

        var existingMenu = new Menu
        {
            Id = 1,
            Name = "Existing Menu",
            Route = "/existing-route",
            ModuleId = 1,
            ParentMenuId = 0,
            Icon = "existing-icon",
            Order = 1
        };
        var menuToUpdate = new Menu
        {
            Id = 2,
            Name = "Menu To Update",
            Route = "/menu-to-update",
            ModuleId = 1,
            ParentMenuId = 0,
            Icon = "update-icon",
            Order = 2
        };
        dbContext.Menus.AddRange(existingMenu, menuToUpdate);
        await dbContext.SaveChangesAsync();

        // SETUP
        var updateMenu = new UpdateMenu(dbContext, mapper);
        var dto = new UpdateMenuDto
        {
            Id = menuToUpdate.Id,
            Name = "Existing Menu", // Nombre duplicado en el mismo módulo
            Route = "/new-route",
            ModuleId = menuToUpdate.ModuleId,
            ParentMenuId = menuToUpdate.ParentMenuId,
            Icon = "new-icon",
            Order = 3
        };

        // ACT
        var result = await updateMenu.Execute(dto);

        // ASSERT
        Assert.True(result.Error != null);
        Assert.Equal("DUPLICATE", result.Error.Code);
    }

}

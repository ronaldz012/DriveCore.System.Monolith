using System;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using Auth.UseCases.mapper;
using Auth.UseCases.Modules;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Modules;

public class AddModuleTest
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
    public async Task AddModule_Success()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext("AddModule_Success_Db");
        var mapper = CreateMapper();
        var addModuleUseCase = new AddModule(dbContext, mapper);

        var createModuleDto = new CreateModuleDto
        {
            Name = "Test Module",
            Description = "This is a test module",
            Menus = new List<CreateMenuForModuleDto>
            {
                new CreateMenuForModuleDto
                {
                    Name = "Menu 1",
                    Route = "/menu1",
                    Icon = "icon1",
                    Order = 1
                },
                new CreateMenuForModuleDto
                {
                    Name = "Menu 2",
                    Route = "/menu2",
                    Icon = "icon2",
                    Order = 2
                }
            }
        };

        // Act
        var result = await addModuleUseCase.Execute(createModuleDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value);

        var addedModule = await dbContext.Modules
            .Include(m => m.Menus)
            .FirstOrDefaultAsync(m => m.Id == result.Value);
        Assert.NotNull(addedModule);
        Assert.Equal("Test Module", addedModule.Name);
        Assert.Equal("This is a test module", addedModule.Description);
        Assert.NotNull(addedModule.Menus);
        Assert.Equal(2, addedModule.Menus.Count);
        Assert.Contains(addedModule.Menus, m => m.Name == "Menu 1");
        Assert.Contains(addedModule.Menus, m => m.Name == "Menu 2");
    }
}

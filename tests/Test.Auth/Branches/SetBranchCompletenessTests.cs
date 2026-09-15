using Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;
using Module.Auth.Domain;

namespace Test.Auth.Branches;

public class SetBranchCompletenessTests
{
    [Fact]
    public async Task Execute_ShouldSetCompleteSince_WhenBranchExists()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = TestAuthDbContextFactory.CreateTenantContext(tenantId);
        using var dbContext = TestAuthDbContextFactory.Create(tenantContext);

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Oruro",
            Place = "Oruro",
            PhoneNumber = "123",
            Type = BranchType.Warehouse,
        };
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var sut = new SetBranchCompleteness(dbContext);
        var completeSince = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = await sut.Execute(branch.Id, new SetBranchCompletenessRequest { CompleteSince = completeSince });

        Assert.True(result.IsSuccess);
        var saved = await dbContext.Branches.FindAsync(branch.Id);
        Assert.NotNull(saved);
        Assert.Equal(completeSince, saved.CompleteSince);
    }

    [Fact]
    public async Task Execute_ShouldReturnBranchNotFound_WhenBranchDoesNotExist()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = TestAuthDbContextFactory.CreateTenantContext(tenantId);
        using var dbContext = TestAuthDbContextFactory.Create(tenantContext);

        var sut = new SetBranchCompleteness(dbContext);

        var result = await sut.Execute(Guid.NewGuid(), new SetBranchCompletenessRequest
        {
            CompleteSince = DateTime.UtcNow
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(SetBranchCompletenessErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldStartAsTransition_WithNullCompleteSince()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = TestAuthDbContextFactory.CreateTenantContext(tenantId);
        using var dbContext = TestAuthDbContextFactory.Create(tenantContext);

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Campero",
            Place = "Campero",
            PhoneNumber = "123",
            Type = BranchType.PointOfSale,
        };
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.Branches.FindAsync(branch.Id);
        Assert.NotNull(saved);
        Assert.Null(saved.CompleteSince);
    }
}

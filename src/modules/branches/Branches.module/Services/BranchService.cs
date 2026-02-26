using Branches.Contracts;
using Branches.Contracts.Dtos;
using Branches.module.Data;
using Branches.module.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Branches.module.Services;

public class BranchService(BranchDbContext context) : IBranchService
{
    public async Task<Result<List<BranchDto>>> GetBranchesByIds(List<int> ids)
    {
        var branches = await context.Branches
            .Where(b => ids.Contains(b.Id) && b.Status)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Status = b.Status,
            }).ToListAsync();

        var foundIds = branches.Select(b => b.Id).ToList();
        var missingIds = ids.Except(foundIds).ToList();

        if (missingIds.Any())
            return new Error("NOT_FOUND", $"Branches not found: {string.Join(", ", missingIds)}");

        return branches;
    }

    public async Task<Result<List<BranchDto>>> GetBranches()
    {
        return await context.Branches.AsNoTracking().Select( x => new BranchDto()
        {
            Id = x.Id,
            Name = x.Name,
            BranchCode =  x.BranchCode,
            Status =  x.Status,
        }).ToListAsync();
    }


    public async Task<Result<bool>> CreateBranch(CreateBranchDto request)
    {
        var newBranch = new Branch
        {
            Name = request.Name,
            Place = request.Place,
            PhoneNumber = request.PhoneNumber,
            Status = true,
            BranchCode = request.BranchCode,
        };
        await context.Branches.AddAsync(newBranch);
        await context.SaveChangesAsync();
        return true;
    }
}
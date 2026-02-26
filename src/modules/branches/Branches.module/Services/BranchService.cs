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
        var branches = await context.Branches.Where(b => ids.Contains(b.Id) && b.Status)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Status = b.Status,
            }).ToListAsync();
        if(!branches.Any())
            return new Error("NOT_FOUND", $"Branch not found with id {ids}");
        
        return branches;
    }

    public Task<Result<List<BranchDto>>> GetBranches()
    {
        throw new NotImplementedException();
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
        return true;
    }
}
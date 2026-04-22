using System;
using Auth.Contracts.Dtos.Users;

namespace Auth.Contracts.Interfaces;

public interface IUserPermissionsCacheService
{
    Task<List<BranchAccessDto>> GetAsync(int userId);
    void Set(int userId, List<BranchAccessDto> branches);
    void Invalidate(int userId);
}

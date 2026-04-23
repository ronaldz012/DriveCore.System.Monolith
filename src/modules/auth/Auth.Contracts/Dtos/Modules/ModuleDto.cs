using Shared.Extensions;

namespace Auth.Contracts.Dtos.Modules;

public class ModuleDto
{
    public int Id { get; set; }
    public string Route {get; set;} = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ModuleQueryDto : GenericPaginationQueryDto
{
    
}
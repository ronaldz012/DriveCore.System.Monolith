using Shared.Extensions;

namespace Auth.Contracts.Dtos.Features;

public class FeatureDto
{
    public int Id { get; set; }
    public string Route {get; set;} = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
}

public class FeatureQueryDto : GenericPaginationQueryDto
{
    
}
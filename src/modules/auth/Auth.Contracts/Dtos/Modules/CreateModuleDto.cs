using System.ComponentModel.DataAnnotations;

namespace Auth.Contracts.Dtos.Modules;

public class CreateModuleDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Route { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [MinLength(1)]
    public IEnumerable<CreateMenuForModuleDto> Menus { get; set; } = Array.Empty<CreateMenuForModuleDto>();
}

public class CreateMenuForModuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
}
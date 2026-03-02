namespace Auth.Contracts.Dtos.Modules;

public class CreateModuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IEnumerable<CreateMenuForModuleDto> Menus { get; set; } = Array.Empty<CreateMenuForModuleDto>();
}

public class CreateMenuForModuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
}
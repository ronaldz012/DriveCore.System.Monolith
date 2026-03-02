namespace Auth.Contracts.Dtos.Modules;

public class CreateMenuDto
{
    public int ParentMenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ModuleId { get; set; }
}

namespace Auth.Data.Entities;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    
    public ICollection<Feature> Features { get; set; } = new List<Feature>();
 }
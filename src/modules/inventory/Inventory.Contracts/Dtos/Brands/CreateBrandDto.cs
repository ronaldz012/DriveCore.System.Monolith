using System.ComponentModel.DataAnnotations;

namespace Inventory.Contracts.Dtos.Brands;

public class CreateBrandDto
{
    [Required, MinLength(3), MaxLength(15)]
    public string Name { get; set; } = string.Empty;
    [Required, MinLength(3), MaxLength(15)]
    public string Description { get; set; } = string.Empty;
}
namespace Branches.Contracts.Dtos;

public class BranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public string BranchCode { get; set; } = string.Empty;
}
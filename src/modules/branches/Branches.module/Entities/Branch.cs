namespace Branches.module.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public string BranchCode { get; set; } = string.Empty;
}
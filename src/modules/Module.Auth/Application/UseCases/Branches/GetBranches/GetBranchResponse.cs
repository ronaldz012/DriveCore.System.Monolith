namespace Module.Auth.Application.UseCases.Branches.GetBranches;

public class GetBranchResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Place { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime? CompleteSince { get; set; }
}
namespace Module.Auth.Application.UseCases.Branches.GetBranchDetails;

public class GetBranchDetailsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompleteSince { get; set; }
}

namespace Auth.Data.Entities;

public class UserBranch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public User User { get; set; } = default!;
}
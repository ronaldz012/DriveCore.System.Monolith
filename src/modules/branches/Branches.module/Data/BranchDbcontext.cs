using Branches.module.Entities;
using Microsoft.EntityFrameworkCore;

namespace Branches.module.Data;

public class BranchDbContext (DbContextOptions<BranchDbContext> options) : DbContext(options)
{
 public DbSet<Branch> Branches { get; set; }   
}
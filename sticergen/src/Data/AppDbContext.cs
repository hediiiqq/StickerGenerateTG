using Microsoft.EntityFrameworkCore;
using sticergen.Data.Models;

namespace sticergen.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
    public DbSet<Draft> Drafts { get; set; }


}
using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ImageRow> Images => Set<ImageRow>();
    public DbSet<VoteRow> Votes => Set<VoteRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ImageRowConfig());
        modelBuilder.ApplyConfiguration(new VoteRowConfig());
    }
}

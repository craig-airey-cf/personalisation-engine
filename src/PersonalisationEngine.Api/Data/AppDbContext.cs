using Microsoft.EntityFrameworkCore;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityAlwaysColumn();
            e.HasIndex(p => p.PlayerId).IsUnique();
            e.Property(p => p.AverageStake).HasColumnType("decimal(10,2)");
            e.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Recommendation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).UseIdentityAlwaysColumn();
            e.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(r => r.Player)
             .WithMany()
             .HasForeignKey(r => r.PlayerId)
             .HasPrincipalKey(p => p.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

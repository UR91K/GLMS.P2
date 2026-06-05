using GLMS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>()
            .Property(c => c.Region)
            .HasMaxLength(80)
            .IsRequired();

        modelBuilder.Entity<Contract>()
            .Property(c => c.ServiceLevel)
            .HasMaxLength(80)
            .IsRequired();

        modelBuilder.Entity<Contract>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ServiceRequest>()
            .Property(sr => sr.Status)
            .HasConversion<string>();
    }
}

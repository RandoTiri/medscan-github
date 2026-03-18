using MedScan.Api.Models;
using MedScan.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser> {
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<UserMedication> UserMedications => Set<UserMedication>();
    public DbSet<DoseLog> DoseLogs => Set<DoseLog>();

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

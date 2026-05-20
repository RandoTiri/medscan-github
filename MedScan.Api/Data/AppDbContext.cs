using MedScan.Api.Data.Identity;
using MedScan.Api.Data.Seed;
using MedScan.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser> {
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<UserMedication> UserMedications => Set<UserMedication>();
    public DbSet<DoseLog> DoseLogs => Set<DoseLog>();
    public DbSet<HomePharmacyItem> HomePharmacyItems => Set<HomePharmacyItem>();

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public async Task SeedMedicationsAsync()
    {
        var now = DateTime.UtcNow;

        var medications = MedicationSeedData.Build(now);

        var seededBarcodes = medications
            .Select(x => x.Barcode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var existing = await Medications.ToListAsync();
        var existingByBarcode = existing
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .ToDictionary(x => x.Barcode, x => x, StringComparer.Ordinal);

        foreach (var seed in medications)
        {
            if (existingByBarcode.TryGetValue(seed.Barcode, out var current))
            {
                current.Name = seed.Name;
                current.ActiveIngredient = seed.ActiveIngredient;
                current.StrengthMg = seed.StrengthMg;
                current.PackSize = seed.PackSize;
                current.Indication = seed.Indication;
                current.Warnings = seed.Warnings;
                current.PdfUrl = seed.PdfUrl;
                current.MethodOfAdministration = seed.MethodOfAdministration;
                current.PrescriptionType = seed.PrescriptionType;
                current.MedicationForm = seed.MedicationForm;
                current.Manufacturer = seed.Manufacturer;
                current.MarketingAuthNumber = seed.MarketingAuthNumber;
                current.CachedAt = now;
            }
            else
            {
                seed.CachedAt = now;
                await Medications.AddAsync(seed);
            }
        }

        var extraRows = existing
            .Where(x => string.IsNullOrWhiteSpace(x.Barcode) || !seededBarcodes.Contains(x.Barcode))
            .ToList();

        if (extraRows.Count > 0)
        {
            Medications.RemoveRange(extraRows);
        }

        await SaveChangesAsync();
    }
}
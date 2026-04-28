using MedScan.Api.Models;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
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
    public DbSet<HomePharmacyItem> HomePharmacyItems => Set<HomePharmacyItem>();

    public async Task SeedMedicationsAsync()
    {
        if (await Medications.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var medications = new List<Medication>
        {
            new()
            {
                Barcode = "4740006010012",
                Name = "Paratsetamool",
                ActiveIngredient = "Paratsetamool",
                StrengthMg = "500",
                Indication = "Valu ja palavik",
                Warnings = "Mitte ületada ööpäevast annust.",
                MedicationForm = "Tablett",
                MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
                PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
                Manufacturer = "Test Pharma",
                MarketingAuthNr = "TEST-001",
                CachedAt = now
            },
            new()
            {
                Barcode = "4740006010029",
                Name = "Ibuprofeen",
                ActiveIngredient = "Ibuprofeen",
                StrengthMg = "400",
                Indication = "Põletik ja valu",
                Warnings = "Võtta koos toiduga.",
                MedicationForm = "Tablett",
                MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
                PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
                Manufacturer = "Test Pharma",
                MarketingAuthNr = "TEST-002",
                CachedAt = now
            },
            new()
            {
                Barcode = "4740006010036",
                Name = "Amoksitsilliin",
                ActiveIngredient = "Amoksitsilliin",
                StrengthMg = "500",
                Indication = "Bakteriaalsed infektsioonid",
                Warnings = "Kasutada arsti juhisel.",
                MedicationForm = "Kapsel",
                MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
                PrescriptionType = PrescriptionTypeEnum.Retseptiravim,
                Manufacturer = "Test Pharma",
                MarketingAuthNr = "TEST-003",
                CachedAt = now
            },
            new()
            {
                Barcode = "4740006010043",
                Name = "Metformiin",
                ActiveIngredient = "Metformiin",
                StrengthMg = "500",
                Indication = "2. tüüpi diabeet",
                Warnings = "Võtta vastavalt raviskeemile.",
                MedicationForm = "Tablett",
                MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
                PrescriptionType = PrescriptionTypeEnum.Retseptiravim,
                Manufacturer = "Test Pharma",
                MarketingAuthNr = "TEST-004",
                CachedAt = now
            },
            new()
            {
                Barcode = "4740006010050",
                Name = "Loratadiin",
                ActiveIngredient = "Loratadiin",
                StrengthMg = "10",
                Indication = "Allergia sümptomid",
                Warnings = "Võib põhjustada uimasust.",
                MedicationForm = "Tablett",
                MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
                PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
                Manufacturer = "Test Pharma",
                MarketingAuthNr = "TEST-005",
                CachedAt = now
            }
        };

        await Medications.AddRangeAsync(medications);
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<Profile>(entity =>
        {
            entity.Property(x => x.ProfileType).HasColumnName("Type");
            entity.HasIndex(x => x.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserMedication>(entity =>
        {
            entity.Property(x => x.Frequency).HasColumnName("FrequencyPerDay");
            entity.Property(x => x.ScheduleUnit).HasConversion<int>();
            entity.Property(x => x.WeeklyDaysJson).HasDefaultValue("[]");
            entity.Property(x => x.StartDate).HasColumnType("date");
        });

        builder.Entity<Medication>(entity =>
        {
            // Current DB schema does not have BestBefore column.
            entity.Ignore(x => x.BestBefore);
            entity.Property(x => x.MarketingAuthNr).HasColumnName("MarketingAuthNumber");
            entity.Property(x => x.MethodOfAdministraion).HasColumnName("MethodOfAdministrion");
        });

        builder.Entity<DoseLog>(entity =>
        {
            entity.Property(x => x.DoseStatus).HasColumnName("Status");
        });

        builder.Entity<HomePharmacyItem>(entity =>
        {
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_HomePharmacyItems_Quantity_Positive", "\"Quantity\" > 0");
            });
            entity.HasIndex(x => x.ProfileId);
            entity.HasIndex(x => x.MedicationId);
            entity.HasIndex(x => x.ExpiresOn);
            entity.Property(x => x.Quantity).HasDefaultValue(1);
            entity.Property(x => x.BatchNumber).HasMaxLength(100);
            entity.Property(x => x.ExpiresOn).HasColumnType("date");
            entity.Property(x => x.AddedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.Profile)
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Medication)
                .WithMany()
                .HasForeignKey(x => x.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

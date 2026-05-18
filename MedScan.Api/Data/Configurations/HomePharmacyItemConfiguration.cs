using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedScan.Api.Data.Configurations {
    public class HomePharmacyItemConfiguration : IEntityTypeConfiguration<HomePharmacyItem> {
        public void Configure(EntityTypeBuilder<HomePharmacyItem> entity) {
            entity.ToTable(table => {
                table.HasCheckConstraint("CK_HomePharmacyItems_Quantity_Positive","\"Quantity\" > 0");
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
        }
    }
}
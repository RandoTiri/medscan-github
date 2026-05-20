using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedScan.Api.Data.Configurations {
    public class MedicationConfiguration : IEntityTypeConfiguration<Medication> {
        public void Configure(EntityTypeBuilder<Medication> entity) {
            entity.Property(x => x.MarketingAuthNumber).HasColumnName("MarketingAuthNumber");
            entity.Property(x => x.MethodOfAdministration).HasColumnName("MethodOfAdministration");
        }
    }
}
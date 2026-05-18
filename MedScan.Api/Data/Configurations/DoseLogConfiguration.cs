using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedScan.Api.Data.Configurations {
    public class DoseLogConfiguration : IEntityTypeConfiguration<DoseLog> {
        public void Configure(EntityTypeBuilder<DoseLog> entity) {
            entity.Property(x => x.DoseStatus).HasColumnName("Status");
        }
    }
}
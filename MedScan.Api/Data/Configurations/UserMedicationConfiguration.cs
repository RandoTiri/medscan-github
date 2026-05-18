using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedScan.Api.Data.Configurations {
    public class UserMedicationConfiguration : IEntityTypeConfiguration<UserMedication> {
        public void Configure(EntityTypeBuilder<UserMedication> entity) {
            entity.Property(x => x.Frequency).HasColumnName("FrequencyPerDay");
            entity.Property(x => x.ScheduleUnit).HasConversion<int>();
            entity.Property(x => x.WeeklyDaysJson).HasDefaultValue("[]");
            entity.Property(x => x.StartDate).HasColumnType("date");
        }
    }
}

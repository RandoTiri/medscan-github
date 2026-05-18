using MedScan.Api.Data.Identity;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedScan.Api.Data.Configurations {
    public class ProfileConfiguration : IEntityTypeConfiguration<Profile> {
        public void Configure(EntityTypeBuilder<Profile> entity) {
            entity.Property(x => x.ProfileType).HasColumnName("Type");
            entity.HasIndex(x => x.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

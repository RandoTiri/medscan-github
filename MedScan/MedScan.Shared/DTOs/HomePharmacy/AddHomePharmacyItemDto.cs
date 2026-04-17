using System.ComponentModel.DataAnnotations;

namespace MedScan.Shared.DTOs.HomePharmacy;

public sealed class AddHomePharmacyItemDto
{
    [Range(1, int.MaxValue)]
    public int ProfileId { get; set; }

    [Range(1, int.MaxValue)]
    public int MedicationId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PackageNumber { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

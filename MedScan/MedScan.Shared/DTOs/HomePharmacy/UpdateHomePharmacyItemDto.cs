using System.ComponentModel.DataAnnotations;

namespace MedScan.Shared.DTOs.HomePharmacy;

public sealed class UpdateHomePharmacyItemDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}


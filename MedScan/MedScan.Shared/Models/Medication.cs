using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models; 
public class Medication {
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public int? StrengthMg { get; set; }
    public string? Indication { get; set; }
    public string? Warnings { get; set; }
    public string? PdfUrl { get; set; }
    public MethodOfAdministrionEnum MethodOfAdministrion { get; set; }
    public PrescriptionTypeEnum PrescriptionType { get; set; }
    public MedicationFormEnum MedicationForm { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string? MarketingAuthNumber { get; set; }
    public DateTime? AuthValidUntil { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
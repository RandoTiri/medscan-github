namespace MedScan.Shared.Models;

public class Medication {
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string? StrengthMg { get; set; }
    public string? PackSize { get; set; }
    public string? Indication { get; set; }
    public string? Warnings { get; set; }
    public string? PdfUrl { get; set; }
    public string MethodOfAdministration { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = string.Empty;
    public string? MedicationForm { get; set; }
    public string? Manufacturer { get; set; } = string.Empty;
    public string? MarketingAuthNumber { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
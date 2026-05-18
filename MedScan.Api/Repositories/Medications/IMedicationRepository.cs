using MedScan.Shared.Models;

namespace MedScan.Api.Repositories.Medications;

public interface IMedicationRepository {
    Task<Medication?> FindByBarcodeAsync(string barcode);
    Task<Medication?> FindByIdAsync(int id);
    Task<IEnumerable<Medication>> SearchByNameAsync(string name);
}

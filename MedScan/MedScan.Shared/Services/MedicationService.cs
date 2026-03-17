using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services; 
public class MedicationService : IMedicationService {
    /*private readonly IMedicationRepository _repo; // Sõltub repo interface'ist
    private readonly IMapper _mapper;

    public MedicationService(IMedicationRepository repo,IMapper mapper) { _repo = repo; _mapper = mapper; }

    public async Task<MedicationDto?> GetByBarcodeAsync(string barcode) {
        var med = await _repo.FindByBarcodeAsync(barcode);
        return med == null ? null : _mapper.Map<MedicationDto>(med);
    }
    // ... ülejäänud meetodid*/
}

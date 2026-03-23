namespace MedScan.Shared.Services;

public class MedicationService {
    /*private readonly IMedicationRepository _repo;
    private readonly IMapper _mapper;

    public MedicationService(IMedicationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<MedicationDto?> GetByBarcodeAsync(string barcode)
    {
        var med = await _repo.FindByBarcodeAsync(barcode);
        return med == null ? null : _mapper.Map<MedicationDto>(med);
    }*/
}

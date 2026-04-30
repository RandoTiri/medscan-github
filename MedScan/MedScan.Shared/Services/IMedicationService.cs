using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services;

public interface IMedicationService {
    Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId, DateOnly? forDate = null);
    Task<UserMedicationDto?> GetByIdAsync(int userMedicationId);
    Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto);
    Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto);
    Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto);
    Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId, DateOnly date);
    Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId, TakeMedicationOnceDto dto);
    Task<bool> RemoveFromScheduleAsync(int userMedicationId);
}

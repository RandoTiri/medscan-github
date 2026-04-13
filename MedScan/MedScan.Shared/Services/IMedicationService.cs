using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services;

public interface IMedicationService {
    Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId);
    Task<UserMedicationDto?> GetByIdAsync(int userMedicationId);
    Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto);
    Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto);
    Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto);
    Task<bool> RemoveFromScheduleAsync(int userMedicationId);
}

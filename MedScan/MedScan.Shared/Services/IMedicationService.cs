using MedScan.Shared.DTOs.Medication;

namespace MedScan.Shared.Services;

public interface IMedicationService {
    Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId);
    Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto);
    Task<UserMedicationDto> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto);
    Task RemoveFromScheduleAsync(int userMedicationId);

}

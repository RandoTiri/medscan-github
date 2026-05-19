using MedScan.Shared.Models;

namespace MedScan.Api.Repositories.UserMedications;

public interface IUserMedicationRepository {
    Task<List<UserMedication>> GetByProfileIdAsync(int profileId);
    Task<UserMedication?> GetByIdAsync(int userMedicationId);
    Task<UserMedication?> GetByIdRawAsync(int userMedicationId);
    Task<List<UserMedication>> GetTrackedActiveByProfileAndMedicationAsync(int profileId,int medicationId);
    Task<UserMedication?> GetTrackedByIdAsync(int userMedicationId);
    Task<UserMedication?> GetTrackedByIdWithLogsAndMedicationAsync(int userMedicationId);
    Task<UserMedication?> GetNewestActiveTrackedByProfileAndMedicationAsync(int profileId,int medicationId);
    Task<bool> IsOwnedByUserAsync(int userMedicationId,string userId,CancellationToken cancellationToken = default);
    Task AddAsync(UserMedication userMedication);
    Task SaveChangesAsync();
}
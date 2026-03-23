using MedScan.Shared.Models;

namespace MedScan.Api.Repositories;

public interface IUserMedicationRepository {
    Task<List<UserMedication>> GetByProfileIdAsync(int profileId);
    Task<UserMedication?> GetByIdAsync(int userMedicationId);
    Task AddAsync(UserMedication userMedication);
    void Remove(UserMedication userMedication);
    Task SaveChangesAsync();
}

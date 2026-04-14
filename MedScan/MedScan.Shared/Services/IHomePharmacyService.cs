using MedScan.Shared.DTOs.HomePharmacy;

namespace MedScan.Shared.Services;

public interface IHomePharmacyService
{
    Task<IReadOnlyList<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId, CancellationToken cancellationToken = default);
    Task<HomePharmacyItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto, CancellationToken cancellationToken = default);
    Task<HomePharmacyItemDto?> UpdateAsync(int id, UpdateHomePharmacyItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default);
}


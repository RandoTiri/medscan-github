using MedScan.Shared.DTOs.HomePharmacy;

namespace MedScan.Api.Services;

public interface IHomePharmacyService
{
    Task<IEnumerable<HomePharmacyItemDto>> GetByProfileIdAsync(int profileId);
    Task<HomePharmacyItemDto?> GetByIdAsync(int id);
    Task<HomePharmacyItemDto> AddAsync(AddHomePharmacyItemDto dto);
    Task<HomePharmacyItemDto?> UpdateAsync(int id, UpdateHomePharmacyItemDto dto);
    Task<bool> RemoveAsync(int id);
}

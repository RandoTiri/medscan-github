using MedScan.Api.Data;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories.Profiles;

public sealed class ProfileRepository(AppDbContext dbContext) : IProfileRepository {
    public Task<bool> ExistsForUserAsync(int profileId,string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == profileId && p.UserId == userId,cancellationToken);
    }

    public Task<Profile?> GetForUserAsync(int profileId,string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId,cancellationToken);
    }

    public Task<Profile?> GetTrackedForUserAsync(int profileId,string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId,cancellationToken);
    }

    public Task<List<Profile>> GetAllForUserAsync(string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ProfileType == ProfileTypeEnum.Ise ? 0 : 1)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Profile>> GetTrackedAllForUserAsync(string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<int?> GetDefaultProfileIdForUserAsync(string userId,CancellationToken cancellationToken) {
        return dbContext.Profiles
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ProfileType == ProfileTypeEnum.Ise ? 0 : 1)
            .ThenBy(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Profile profile,CancellationToken cancellationToken) {
        await dbContext.Profiles.AddAsync(profile,cancellationToken);
    }

    public void Remove(Profile profile) {
        dbContext.Profiles.Remove(profile);
    }

    public void RemoveRange(IEnumerable<Profile> profiles) {
        dbContext.Profiles.RemoveRange(profiles);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
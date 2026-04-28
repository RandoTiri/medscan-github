using System.Text.Json;
using MedScan.Api.Repositories;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.Api.Services;

public sealed class MedicationService : IMedicationService
{
    private readonly IUserMedicationRepository _userMedicationRepository;
    private readonly IMedicationRepository _medicationRepository;

    public MedicationService(
        IUserMedicationRepository userMedicationRepository,
        IMedicationRepository medicationRepository)
    {
        _userMedicationRepository = userMedicationRepository;
        _medicationRepository = medicationRepository;
    }

    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId, DateOnly? forDate = null)
    {
        var items = await _userMedicationRepository.GetByProfileIdAsync(profileId);
        return items.Select(item => MapToDto(item, forDate));
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId)
    {
        var item = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        return item is null ? null : MapToDto(item);
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto)
    {
        ValidateAddMedication(dto);

        var medication = await _medicationRepository.FindByIdAsync(dto.MedicationId);
        if (medication is null)
        {
            throw new InvalidOperationException($"Medication with id {dto.MedicationId} was not found.");
        }

        var normalizedTimes = dto.ScheduledTimes
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var userMedication = new UserMedication
        {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            Frequency = dto.FrequencyPerDay,
            ScheduledTimesJson = SerializeTimes(normalizedTimes),
            ExpiresOn = dto.ExpiresOn,
            RemindersEnabled = dto.RemindersEnabled,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            IsActive = true,
            AddedAt = DateTime.UtcNow
        };

        await _userMedicationRepository.AddAsync(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        var created = await _userMedicationRepository.GetByIdAsync(userMedication.Id)
            ?? throw new InvalidOperationException("Created medication could not be reloaded.");

        return MapToDto(created);
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId, AddMedicationDto dto)
    {
        ValidateAddMedication(dto);

        var userMedication = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return null;
        }

        var normalizedTimes = dto.ScheduledTimes
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        userMedication.ProfileId = dto.ProfileId;
        userMedication.MedicationId = dto.MedicationId;
        userMedication.Frequency = dto.FrequencyPerDay;
        userMedication.ScheduledTimesJson = SerializeTimes(normalizedTimes);
        userMedication.ExpiresOn = dto.ExpiresOn;
        userMedication.RemindersEnabled = dto.RemindersEnabled;
        userMedication.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        await _userMedicationRepository.SaveChangesAsync();

        var updated = await _userMedicationRepository.GetByIdAsync(userMedicationId)
            ?? throw new InvalidOperationException("Updated medication could not be reloaded.");

        return MapToDto(updated);
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId)
    {
        var userMedication = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return false;
        }

        _userMedicationRepository.Remove(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        return true;
    }

    public Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto)
    {
        throw new NotSupportedException("Status updates are handled by MedicationsController.");
    }

    private static UserMedicationDto MapToDto(UserMedication userMedication, DateOnly? forDate = null)
    {
        var scheduledTimes = DeserializeTimes(userMedication.ScheduledTimesJson);
        var doseStatuses = BuildDoseStatuses(userMedication, scheduledTimes, forDate);

        var latestStatus = userMedication.DoseLogs
            .OrderByDescending(log => log.ScheduledTime)
            .ThenByDescending(log => log.Id)
            .Select(log => (DoseStatusEnum?)log.DoseStatus)
            .FirstOrDefault();

        return new UserMedicationDto
        {
            Id = userMedication.Id,
            ProfileId = userMedication.ProfileId,
            ProfileName = userMedication.Profile?.Name ?? string.Empty,
            MedicationId = userMedication.MedicationId,
            MedicationName = userMedication.Medication?.Name ?? string.Empty,
            Strength = userMedication.Medication?.StrengthMg,
            FrequencyPerDay = userMedication.Frequency,
            ScheduledTimes = scheduledTimes,
            TodayDoses = doseStatuses,
            ExpiresOn = userMedication.ExpiresOn,
            RemindersEnabled = userMedication.RemindersEnabled,
            Notes = userMedication.Notes,
            IsActive = userMedication.IsActive,
            LatestDoseStatus = latestStatus
        };
    }

    private static List<ScheduledDoseStatusDto> BuildDoseStatuses(UserMedication userMedication, List<TimeOnly> scheduledTimes, DateOnly? forDate = null)
    {
        var nowLocal = DateTime.Now;
        var selectedDate = forDate ?? DateOnly.FromDateTime(nowLocal);
        var selectedLocalDate = selectedDate.ToDateTime(TimeOnly.MinValue);
        var addedLocalDate = userMedication.AddedAt.ToLocalTime().Date;

        if (addedLocalDate > selectedLocalDate)
        {
            return [];
        }

        var logsByScheduledUtc = userMedication.DoseLogs
            .Where(log => DateOnly.FromDateTime(log.ScheduledTime.ToLocalTime()) == selectedDate)
            .GroupBy(log => log.ScheduledTime)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(log => log.Id).First().DoseStatus);

        var result = new List<ScheduledDoseStatusDto>(scheduledTimes.Count);

        foreach (var scheduledTime in scheduledTimes.OrderBy(time => time))
        {
            var localScheduledDateTime = DateTime.SpecifyKind(selectedLocalDate.Add(scheduledTime.ToTimeSpan()), DateTimeKind.Local);
            var scheduledUtc = localScheduledDateTime.ToUniversalTime();

            if (logsByScheduledUtc.TryGetValue(scheduledUtc, out var statusFromLog))
            {
                result.Add(new ScheduledDoseStatusDto
                {
                    ScheduledTime = scheduledTime,
                    Status = statusFromLog
                });
                continue;
            }

            var computedStatus = selectedDate < DateOnly.FromDateTime(nowLocal)
                ? DoseStatusEnum.Missed
                : localScheduledDateTime <= nowLocal
                    ? DoseStatusEnum.Missed
                    : DoseStatusEnum.Pending;

            result.Add(new ScheduledDoseStatusDto
            {
                ScheduledTime = scheduledTime,
                Status = computedStatus
            });
        }

        return result;
    }

    private static string SerializeTimes(List<TimeOnly> times)
    {
        return JsonSerializer.Serialize(times);
    }

    private static void ValidateAddMedication(AddMedicationDto dto)
    {
        if (dto.ProfileId <= 0)
        {
            throw new InvalidOperationException("ProfileId on vigane.");
        }

        if (dto.MedicationId <= 0)
        {
            throw new InvalidOperationException("MedicationId on vigane.");
        }

        if (dto.FrequencyPerDay <= 0 || dto.FrequencyPerDay > 24)
        {
            throw new InvalidOperationException("Manustamissagedus peab olema vahemikus 1-24.");
        }

        if (dto.ScheduledTimes is null || dto.ScheduledTimes.Count == 0)
        {
            throw new InvalidOperationException("Vähemalt üks kellaaeg on kohustuslik.");
        }

        if (dto.ScheduledTimes.Count != dto.FrequencyPerDay)
        {
            throw new InvalidOperationException("Kellaaegade arv peab vastama manustamissagedusele.");
        }

        if (dto.ScheduledTimes.Distinct().Count() != dto.ScheduledTimes.Count)
        {
            throw new InvalidOperationException("Kellaajad peavad olema erinevad.");
        }
    }

    private static List<TimeOnly> DeserializeTimes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TimeOnly>>(json) ?? [];
    }
}

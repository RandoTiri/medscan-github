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

        var normalizedTimes = NormalizeTimes(dto.ScheduledTimes);
        var normalizedWeeklyDays = NormalizeWeeklyDays(dto.ScheduleUnit, dto.FrequencyPerDay, dto.WeeklyDays);
        var startDate = dto.StartDate ?? DateOnly.FromDateTime(DateTime.Now);

        var existingActiveSchedules = await _userMedicationRepository
            .GetTrackedActiveByProfileAndMedicationAsync(dto.ProfileId, dto.MedicationId);
        foreach (var existing in existingActiveSchedules)
        {
            existing.IsActive = false;
        }

        var userMedication = new UserMedication
        {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            Frequency = dto.FrequencyPerDay,
            ScheduleUnit = dto.ScheduleUnit,
            ScheduledTimesJson = SerializeTimes(normalizedTimes),
            WeeklyDaysJson = SerializeWeeklyDays(normalizedWeeklyDays),
            StartDate = startDate,
            ExpiresOn = dto.ExpiresOn,
            RemindersEnabled = dto.RemindersEnabled,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            IsActive = true,
            AddedAt = DateTime.UtcNow
        };

        await _userMedicationRepository.AddAsync(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        var created = await _userMedicationRepository.GetByIdRawAsync(userMedication.Id)
            ?? throw new InvalidOperationException("Created medication could not be reloaded.");

        return MapToDto(created);
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId, AddMedicationDto dto)
    {
        ValidateAddMedication(dto);

        var userMedication = await _userMedicationRepository.GetTrackedByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return null;
        }

        var normalizedTimes = NormalizeTimes(dto.ScheduledTimes);
        var normalizedWeeklyDays = NormalizeWeeklyDays(dto.ScheduleUnit, dto.FrequencyPerDay, dto.WeeklyDays);

        userMedication.ProfileId = dto.ProfileId;
        userMedication.MedicationId = dto.MedicationId;
        userMedication.Frequency = dto.FrequencyPerDay;
        userMedication.ScheduleUnit = dto.ScheduleUnit;
        userMedication.ScheduledTimesJson = SerializeTimes(normalizedTimes);
        userMedication.WeeklyDaysJson = SerializeWeeklyDays(normalizedWeeklyDays);
        userMedication.StartDate = dto.StartDate ?? userMedication.StartDate;
        userMedication.ExpiresOn = dto.ExpiresOn;
        userMedication.RemindersEnabled = dto.RemindersEnabled;
        userMedication.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        await _userMedicationRepository.SaveChangesAsync();

        var updated = await _userMedicationRepository.GetByIdRawAsync(userMedicationId)
            ?? throw new InvalidOperationException("Updated medication could not be reloaded.");

        return MapToDto(updated);
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId)
    {
        var userMedication = await _userMedicationRepository.GetTrackedByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return false;
        }

        userMedication.IsActive = false;
        await _userMedicationRepository.SaveChangesAsync();

        return true;
    }

    public Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto)
    {
        throw new NotSupportedException("Status updates are handled by MedicationsController.");
    }

    public Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId, DateOnly date)
    {
        throw new NotSupportedException("History is handled by MedicationsController.");
    }

    public Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId, TakeMedicationOnceDto dto)
    {
        throw new NotSupportedException("One-time take is handled by MedicationsController.");
    }

    private static UserMedicationDto MapToDto(UserMedication userMedication, DateOnly? forDate = null)
    {
        var scheduledTimes = DeserializeTimes(userMedication.ScheduledTimesJson);
        var weeklyDays = DeserializeWeeklyDays(userMedication.WeeklyDaysJson);
        var doseStatuses = BuildDoseStatuses(userMedication, scheduledTimes, weeklyDays, forDate);

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
            ScheduleUnit = userMedication.ScheduleUnit,
            ScheduledTimes = scheduledTimes,
            WeeklyDays = weeklyDays,
            StartDate = userMedication.StartDate,
            TodayDoses = doseStatuses,
            ExpiresOn = userMedication.ExpiresOn,
            RemindersEnabled = userMedication.RemindersEnabled,
            Notes = userMedication.Notes,
            IsActive = userMedication.IsActive,
            LatestDoseStatus = latestStatus
        };
    }

    private static List<ScheduledDoseStatusDto> BuildDoseStatuses(
        UserMedication userMedication,
        List<TimeOnly> scheduledTimes,
        List<int> weeklyDays,
        DateOnly? forDate = null)
    {
        var nowLocal = DateTime.Now;
        var selectedDate = forDate ?? DateOnly.FromDateTime(nowLocal);
        var selectedLocalDate = selectedDate.ToDateTime(TimeOnly.MinValue);

        var occurrences = MedicationScheduleCalculator.GetOccurrencesForDate(
            userMedication.ScheduleUnit,
            userMedication.Frequency,
            userMedication.StartDate,
            scheduledTimes,
            weeklyDays,
            selectedDate);

        if (occurrences.Count == 0)
        {
            return [];
        }

        var logsByScheduledUtc = userMedication.DoseLogs
            .Where(log => DateOnly.FromDateTime(log.ScheduledTime.ToLocalTime()) == selectedDate)
            .GroupBy(log => log.ScheduledTime)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(log => log.Id).First().DoseStatus);

        var result = new List<ScheduledDoseStatusDto>(occurrences.Count);

        foreach (var occurrence in occurrences)
        {
            var localScheduledDateTime = DateTime.SpecifyKind(
                selectedLocalDate.Add(occurrence.Time.ToTimeSpan()),
                DateTimeKind.Local);
            var scheduledUtc = localScheduledDateTime.ToUniversalTime();

            if (logsByScheduledUtc.TryGetValue(scheduledUtc, out var statusFromLog))
            {
                result.Add(new ScheduledDoseStatusDto
                {
                    ScheduledTime = occurrence.Time,
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
                ScheduledTime = occurrence.Time,
                Status = computedStatus
            });
        }

        return result;
    }

    private static List<TimeOnly> NormalizeTimes(List<TimeOnly> times)
    {
        return times
            .Select((time, index) => new { time, index })
            .OrderBy(x => x.index)
            .Select(x => x.time)
            .ToList();
    }

    private static List<int> NormalizeWeeklyDays(MedicationScheduleUnit unit, int frequency, List<int>? weeklyDays)
    {
        if (unit != MedicationScheduleUnit.Week || frequency <= 1)
        {
            return [];
        }

        return (weeklyDays ?? [])
            .Take(frequency)
            .ToList();
    }

    private static string SerializeTimes(List<TimeOnly> times)
    {
        return JsonSerializer.Serialize(times);
    }

    private static string SerializeWeeklyDays(List<int> weeklyDays)
    {
        return JsonSerializer.Serialize(weeklyDays);
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

        var expectedTimeCount = dto.ScheduleUnit == MedicationScheduleUnit.Month
            ? 1
            : dto.FrequencyPerDay;

        if (dto.ScheduledTimes.Count != expectedTimeCount)
        {
            throw new InvalidOperationException("Kellaaegade arv peab vastama manustamissagedusele.");
        }

        if (dto.ScheduleUnit == MedicationScheduleUnit.Week && dto.FrequencyPerDay > 1)
        {
            if (dto.WeeklyDays.Count != dto.FrequencyPerDay)
            {
                throw new InvalidOperationException("Vali iga nädalase manustamise jaoks päev.");
            }

            if (dto.WeeklyDays.Any(day => day < 0 || day > 6))
            {
                throw new InvalidOperationException("Nädalapäevad on vigased.");
            }

            if (dto.WeeklyDays.Distinct().Count() != dto.WeeklyDays.Count)
            {
                throw new InvalidOperationException("Nädalapäevad peavad olema erinevad.");
            }
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

    private static List<int> DeserializeWeeklyDays(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<int>>(json) ?? [];
    }
}

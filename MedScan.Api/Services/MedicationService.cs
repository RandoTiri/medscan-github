using System.Text.Json;
using MedScan.Api.Data;
using MedScan.Api.Repositories;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Services;

public sealed class MedicationService : IMedicationService
{
    private readonly IUserMedicationRepository _userMedicationRepository;
    private readonly IMedicationRepository _medicationRepository;
    private readonly AppDbContext _dbContext;

    public MedicationService(
        IUserMedicationRepository userMedicationRepository,
        IMedicationRepository medicationRepository,
        AppDbContext dbContext)
    {
        _userMedicationRepository = userMedicationRepository;
        _medicationRepository = medicationRepository;
        _dbContext = dbContext;
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

    public async Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto)
    {
        var userMedication = await _dbContext.UserMedications
            .Include(um => um.Medication)
            .Include(um => um.DoseLogs)
            .FirstOrDefaultAsync(um => um.Id == userMedicationId);

        if (userMedication is null)
        {
            return null;
        }

        var nowLocal = DateTime.Now;
        var localDate = nowLocal.Date;
        var resolvedTime = dto.ScheduledTime ?? TimeOnly.FromDateTime(nowLocal);
        var scheduledUtc = DateTime.SpecifyKind(localDate.Add(resolvedTime.ToTimeSpan()), DateTimeKind.Local)
            .ToUniversalTime();

        var existingLog = userMedication.DoseLogs
            .Where(log => log.ScheduledTime == scheduledUtc)
            .OrderByDescending(log => log.Id)
            .FirstOrDefault();

        var previousStatus = existingLog?.DoseStatus;
        if (existingLog is null)
        {
            existingLog = new DoseLog
            {
                UserMedicationId = userMedication.Id,
                ScheduledTime = scheduledUtc,
                DoseStatus = dto.Status,
                TakenAt = dto.Status == DoseStatusEnum.Done ? DateTime.UtcNow : null
            };
            _dbContext.DoseLogs.Add(existingLog);
        }
        else
        {
            existingLog.DoseStatus = dto.Status;
            existingLog.TakenAt = dto.Status == DoseStatusEnum.Done ? DateTime.UtcNow : null;
        }

        var stockWarning = string.Empty;
        int? remainingQuantity = null;
        var removedFromEverywhere = false;
        var shouldDecreaseStock = dto.Status == DoseStatusEnum.Done && previousStatus != DoseStatusEnum.Done;
        if (shouldDecreaseStock)
        {
            var stockItem = await _dbContext.HomePharmacyItems
                .Where(item => item.ProfileId == userMedication.ProfileId && item.MedicationId == userMedication.MedicationId)
                .OrderByDescending(item => item.AddedAt)
                .FirstOrDefaultAsync();

            if (stockItem is not null && stockItem.Quantity > 0)
            {
                if (stockItem.Quantity == 1)
                {
                    _dbContext.HomePharmacyItems.Remove(stockItem);
                    remainingQuantity = 0;
                    removedFromEverywhere = true;
                }
                else
                {
                    stockItem.Quantity -= 1;
                    remainingQuantity = stockItem.Quantity;
                }

                if (remainingQuantity <= 5)
                {
                    stockWarning = remainingQuantity == 0
                        ? "PAKI VIIMANE RAVIM. Pärast märkimist kustub see raviskeemist ja ravimite nimekirjast. Kui jätkad  võtmist, skänni uus karp ja lisa ravim uuesti enda raviskeemi."
                        : $"NB seda ravimit on alles vaid {remainingQuantity} tk. Kui jätkad sama raviskeemi, osta uus karp.";
                }
            }
        }

        if (removedFromEverywhere)
        {
            var allActiveSchedules = await _dbContext.UserMedications
                .Where(um =>
                    um.ProfileId == userMedication.ProfileId &&
                    um.MedicationId == userMedication.MedicationId &&
                    um.IsActive)
                .ToListAsync();

            foreach (var schedule in allActiveSchedules)
            {
                schedule.IsActive = false;
            }
        }

        await _dbContext.SaveChangesAsync();

        if (removedFromEverywhere)
        {
            return new UserMedicationDto
            {
                Id = userMedicationId,
                ProfileId = userMedication.ProfileId,
                MedicationId = userMedication.MedicationId,
                MedicationName = userMedication.Medication?.Name ?? string.Empty,
                IsActive = false,
                RemainingQuantity = 0,
                StockWarning = string.IsNullOrWhiteSpace(stockWarning) ? null : stockWarning
            };
        }

        var updated = await GetByIdAsync(userMedicationId);
        if (updated is null)
        {
            return null;
        }

        updated.RemainingQuantity = remainingQuantity;
        updated.StockWarning = string.IsNullOrWhiteSpace(stockWarning) ? null : stockWarning;
        return updated;
    }

    public async Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId, DateOnly date)
    {
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        var utcStart = localStart.ToUniversalTime();
        var utcEnd = localEnd.ToUniversalTime();

        return await _dbContext.DoseLogs
            .AsNoTracking()
            .Include(log => log.UserMedication)
                .ThenInclude(um => um.Medication)
            .Where(log =>
                log.UserMedication.ProfileId == profileId &&
                log.ScheduledTime >= utcStart &&
                log.ScheduledTime < utcEnd)
            .OrderBy(log => log.ScheduledTime)
            .Select(log => new DoseHistoryItemDto
            {
                MedicationName = log.UserMedication.Medication.Name,
                Strength = log.UserMedication.Medication.StrengthMg,
                ScheduledTime = log.ScheduledTime,
                Status = log.DoseStatus
            })
            .ToListAsync();
    }

    public async Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId, TakeMedicationOnceDto dto)
    {
        if (dto.ProfileId <= 0)
        {
            return new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Profiil puudub."
            };
        }

        var quantityToTake = dto.Quantity <= 0 ? 1 : dto.Quantity;
        var medication = await _dbContext.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medicationId);

        if (medication is null)
        {
            return new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Ravimit ei leitud."
            };
        }

        var stockItem = await _dbContext.HomePharmacyItems
            .Where(item => item.ProfileId == dto.ProfileId && item.MedicationId == medicationId)
            .OrderByDescending(item => item.AddedAt)
            .FirstOrDefaultAsync();

        if (stockItem is null || stockItem.Quantity <= 0)
        {
            return new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = "Ravimit ei ole koduses varus."
            };
        }

        if (stockItem.Quantity == 1 && !dto.ConfirmLastUnit)
        {
            return new TakeMedicationOnceResultDto
            {
                Success = false,
                RequiresLastUnitConfirmation = true,
                RemainingQuantity = 1,
                Message = "See on viimane ühik. Võtmisega jätkates pead uue paki ostma."
            };
        }

        if (stockItem.Quantity < quantityToTake)
        {
            return new TakeMedicationOnceResultDto
            {
                Success = false,
                Message = $"Kodus on alles {stockItem.Quantity} tk. Vähenda võetavat kogust."
            };
        }

        var takenAt = DateTime.UtcNow;
        var activeMedication = await _dbContext.UserMedications
            .Where(um => um.ProfileId == dto.ProfileId && um.MedicationId == medicationId && um.IsActive)
            .OrderByDescending(um => um.Id)
            .FirstOrDefaultAsync();

        var medicationForLog = activeMedication;
        if (medicationForLog is null)
        {
            medicationForLog = new UserMedication
            {
                ProfileId = dto.ProfileId,
                MedicationId = medicationId,
                Frequency = 1,
                ScheduleUnit = MedicationScheduleUnit.Day,
                ScheduledTimesJson = "[\"08:00:00\"]",
                WeeklyDaysJson = "[]",
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                RemindersEnabled = false,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? "Ühekordne võtmine" : dto.Notes.Trim(),
                IsActive = false,
                AddedAt = DateTime.UtcNow
            };

            _dbContext.UserMedications.Add(medicationForLog);
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.DoseLogs.Add(new DoseLog
        {
            UserMedicationId = medicationForLog.Id,
            ScheduledTime = takenAt,
            TakenAt = takenAt,
            DoseStatus = DoseStatusEnum.Done
        });

        var remaining = stockItem.Quantity - quantityToTake;
        var removed = remaining <= 0;
        if (removed)
        {
            _dbContext.HomePharmacyItems.Remove(stockItem);
        }
        else
        {
            stockItem.Quantity = remaining;
        }

        await _dbContext.SaveChangesAsync();

        return new TakeMedicationOnceResultDto
        {
            Success = true,
            RemainingQuantity = removed ? 0 : remaining,
            RemovedFromHomePharmacy = removed,
            Message = "Salvestatud logisse, meeldetuletust ei looda."
        };
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

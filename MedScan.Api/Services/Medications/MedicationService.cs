using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Medications;
using MedScan.Api.Services.Medications;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.Api.Services;

public sealed class MedicationService : IMedicationService {
    private readonly IUserMedicationRepository _userMedicationRepository;
    private readonly IMedicationRepository _medicationRepository;
    private readonly IDoseLogRepository _doseLogRepository;
    private readonly IHomePharmacyRepository _homePharmacyRepository;

    public MedicationService(
        IUserMedicationRepository userMedicationRepository,
        IMedicationRepository medicationRepository,
        IDoseLogRepository doseLogRepository,
        IHomePharmacyRepository homePharmacyRepository) {
        _userMedicationRepository = userMedicationRepository;
        _medicationRepository = medicationRepository;
        _doseLogRepository = doseLogRepository;
        _homePharmacyRepository = homePharmacyRepository;
    }

    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId,DateOnly? forDate = null) {
        var items = await _userMedicationRepository.GetByProfileIdAsync(profileId);
        return items.Select(item => MedicationMapper.ToDto(item,forDate));
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId) {
        var item = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        return item is null ? null : MedicationMapper.ToDto(item);
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto) {
        MedicationValidator.EnsureValid(dto);

        var medication = await _medicationRepository.FindByIdAsync(dto.MedicationId)
            ?? throw new InvalidOperationException(
                string.Format(MedicationConstants.Messages.MedicationNotFoundById,dto.MedicationId));

        await DeactivateActiveSchedulesAsync(dto.ProfileId,dto.MedicationId);

        var userMedication = BuildUserMedicationFromDto(dto);
        await _userMedicationRepository.AddAsync(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        var created = await _userMedicationRepository.GetByIdRawAsync(userMedication.Id)
            ?? throw new InvalidOperationException(MedicationConstants.Messages.CreatedReloadFailed);

        return MedicationMapper.ToDto(created);
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto) {
        MedicationValidator.EnsureValid(dto);

        var userMedication = await _userMedicationRepository.GetTrackedByIdAsync(userMedicationId);
        if (userMedication is null) {
            return null;
        }

        ApplyDtoToEntity(dto,userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        var updated = await _userMedicationRepository.GetByIdRawAsync(userMedicationId)
            ?? throw new InvalidOperationException(MedicationConstants.Messages.UpdatedReloadFailed);

        return MedicationMapper.ToDto(updated);
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId) {
        var userMedication = await _userMedicationRepository.GetTrackedByIdAsync(userMedicationId);
        if (userMedication is null) {
            return false;
        }

        userMedication.IsActive = false;
        await _userMedicationRepository.SaveChangesAsync();

        return true;
    }

    public async Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId,UpdateMedicationStatusDto dto) {
        var userMedication = await _userMedicationRepository.GetTrackedByIdWithLogsAndMedicationAsync(userMedicationId);
        if (userMedication is null) {
            return null;
        }

        var scheduledUtc = ResolveScheduledUtc(dto.ScheduledTime);
        var previousStatus = UpsertDoseLog(userMedication,scheduledUtc,dto.Status);

        var stockResult = await TryDecrementStockForDoseAsync(userMedication,dto.Status,previousStatus);

        if (stockResult.RemovedFromEverywhere) {
            await DeactivateActiveSchedulesAsync(userMedication.ProfileId,userMedication.MedicationId);
        }

        await _userMedicationRepository.SaveChangesAsync();

        return await BuildUpdateStatusResultAsync(userMedicationId,userMedication,stockResult);
    }

    public async Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId,DateOnly date) {
        var (utcStart,utcEnd) = GetUtcDateRange(date);
        var logs = await _doseLogRepository.GetByProfileInRangeAsync(profileId,utcStart,utcEnd);

        return logs.Select(log => new DoseHistoryItemDto {
            MedicationName = log.UserMedication.Medication.Name,
            Strength = log.UserMedication.Medication.StrengthMg,
            ScheduledTime = log.ScheduledTime,
            Status = log.DoseStatus
        });
    }

    public async Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId,TakeMedicationOnceDto dto) {
        if (dto.ProfileId <= 0) {
            return Failure(MedicationConstants.Messages.ProfileMissing);
        }

        var medication = await _medicationRepository.FindByIdAsync(medicationId);
        if (medication is null) {
            return Failure(MedicationConstants.Messages.MedicationNotFound);
        }

        var stockItem = await _homePharmacyRepository
            .FindNewestTrackedByProfileAndMedicationAsync(dto.ProfileId,medicationId);
        var stockCheck = ValidateStockForTakeOnce(stockItem,dto);
        if (stockCheck is not null) {
            return stockCheck;
        }

        var medicationForLog = await EnsureUserMedicationForLogAsync(dto.ProfileId,medicationId,dto.Notes);
        LogTakenDose(medicationForLog.Id);

        var quantityToTake = NormalizeQuantity(dto.Quantity);
        var (remaining,removed) = ConsumeStock(stockItem!,quantityToTake);

        await _userMedicationRepository.SaveChangesAsync();

        return new TakeMedicationOnceResultDto {
            Success = true,
            RemainingQuantity = removed ? 0 : remaining,
            RemovedFromHomePharmacy = removed,
            Message = MedicationConstants.Messages.TakeOnceSaved
        };
    }

    private async Task DeactivateActiveSchedulesAsync(int profileId,int medicationId) {
        var activeSchedules = await _userMedicationRepository
            .GetTrackedActiveByProfileAndMedicationAsync(profileId,medicationId);

        foreach (var schedule in activeSchedules) {
            schedule.IsActive = false;
        }
    }

    private static UserMedication BuildUserMedicationFromDto(AddMedicationDto dto) {
        var normalizedWeeklyDays = MedicationValidator.NormalizeWeeklyDays(
            dto.ScheduleUnit,dto.FrequencyPerDay,dto.WeeklyDays);

        return new UserMedication {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            Frequency = dto.FrequencyPerDay,
            ScheduleUnit = dto.ScheduleUnit,
            ScheduledTimesJson = MedicationScheduleSerializer.SerializeTimes(dto.ScheduledTimes),
            WeeklyDaysJson = MedicationScheduleSerializer.SerializeWeeklyDays(normalizedWeeklyDays),
            StartDate = dto.StartDate ?? DateOnly.FromDateTime(DateTime.Now),
            RemindersEnabled = dto.RemindersEnabled,
            Notes = TrimNotesOrNull(dto.Notes),
            IsActive = true,
            AddedAt = DateTime.UtcNow
        };
    }

    private static void ApplyDtoToEntity(AddMedicationDto dto,UserMedication entity) {
        var normalizedWeeklyDays = MedicationValidator.NormalizeWeeklyDays(
            dto.ScheduleUnit,dto.FrequencyPerDay,dto.WeeklyDays);

        entity.ProfileId = dto.ProfileId;
        entity.MedicationId = dto.MedicationId;
        entity.Frequency = dto.FrequencyPerDay;
        entity.ScheduleUnit = dto.ScheduleUnit;
        entity.ScheduledTimesJson = MedicationScheduleSerializer.SerializeTimes(dto.ScheduledTimes);
        entity.WeeklyDaysJson = MedicationScheduleSerializer.SerializeWeeklyDays(normalizedWeeklyDays);
        entity.StartDate = dto.StartDate ?? entity.StartDate;
        entity.RemindersEnabled = dto.RemindersEnabled;
        entity.Notes = TrimNotesOrNull(dto.Notes);
    }

    private static DateTime ResolveScheduledUtc(TimeOnly? scheduledTime) {
        var nowLocal = DateTime.Now;
        var resolvedTime = scheduledTime ?? TimeOnly.FromDateTime(nowLocal);
        return DateTime
            .SpecifyKind(nowLocal.Date.Add(resolvedTime.ToTimeSpan()),DateTimeKind.Local)
            .ToUniversalTime();
    }

    private DoseStatusEnum? UpsertDoseLog(UserMedication userMedication,DateTime scheduledUtc,DoseStatusEnum status) {
        var existingLog = userMedication.DoseLogs
            .Where(log => log.ScheduledTime == scheduledUtc)
            .OrderByDescending(log => log.Id)
            .FirstOrDefault();

        var previousStatus = existingLog?.DoseStatus;
        var takenAt = status == DoseStatusEnum.Done ? DateTime.UtcNow : (DateTime?)null;

        if (existingLog is null) {
            _doseLogRepository.Add(new DoseLog {
                UserMedicationId = userMedication.Id,
                ScheduledTime = scheduledUtc,
                DoseStatus = status,
                TakenAt = takenAt
            });
        } else {
            existingLog.DoseStatus = status;
            existingLog.TakenAt = takenAt;
        }

        return previousStatus;
    }

    private async Task<StockDecrementResult> TryDecrementStockForDoseAsync(
        UserMedication userMedication,
        DoseStatusEnum newStatus,
        DoseStatusEnum? previousStatus) {
        var shouldDecrease = newStatus == DoseStatusEnum.Done && previousStatus != DoseStatusEnum.Done;
        if (!shouldDecrease) {
            return StockDecrementResult.NotApplicable;
        }

        var stockItem = await _homePharmacyRepository
            .FindNewestTrackedByProfileAndMedicationAsync(userMedication.ProfileId,userMedication.MedicationId);
        if (stockItem is null || stockItem.Quantity <= 0) {
            return StockDecrementResult.NotApplicable;
        }

        if (stockItem.Quantity == 1) {
            _homePharmacyRepository.Remove(stockItem);
            return new StockDecrementResult(
                RemainingQuantity: 0,
                RemovedFromEverywhere: true,
                Warning: MedicationConstants.Messages.LastPackWarning);
        }

        stockItem.Quantity -= 1;
        var remaining = stockItem.Quantity;
        var warning = remaining <= MedicationConstants.LowStockWarningThreshold
            ? MedicationConstants.Messages.LowStockRemaining(remaining)
            : null;

        return new StockDecrementResult(remaining,RemovedFromEverywhere: false,Warning: warning);
    }

    private async Task<UserMedicationDto?> BuildUpdateStatusResultAsync(
        int userMedicationId,
        UserMedication userMedication,
        StockDecrementResult stockResult) {
        if (stockResult.RemovedFromEverywhere) {
            return new UserMedicationDto {
                Id = userMedicationId,
                ProfileId = userMedication.ProfileId,
                MedicationId = userMedication.MedicationId,
                MedicationName = userMedication.Medication?.Name ?? string.Empty,
                IsActive = false,
                RemainingQuantity = 0,
                StockWarning = stockResult.Warning
            };
        }

        var updated = await GetByIdAsync(userMedicationId);
        if (updated is null) {
            return null;
        }

        updated.RemainingQuantity = stockResult.RemainingQuantity;
        updated.StockWarning = stockResult.Warning;
        return updated;
    }

    private static TakeMedicationOnceResultDto? ValidateStockForTakeOnce(HomePharmacyItem? stockItem,TakeMedicationOnceDto dto) {
        if (stockItem is null || stockItem.Quantity <= 0) {
            return Failure(MedicationConstants.Messages.StockEmpty);
        }

        if (stockItem.Quantity == 1 && !dto.ConfirmLastUnit) {
            return new TakeMedicationOnceResultDto {
                Success = false,
                RequiresLastUnitConfirmation = true,
                RemainingQuantity = 1,
                Message = MedicationConstants.Messages.LastUnitConfirmationNeeded
            };
        }

        var quantityToTake = NormalizeQuantity(dto.Quantity);
        if (stockItem.Quantity < quantityToTake) {
            return Failure(MedicationConstants.Messages.StockBelowRequested(stockItem.Quantity));
        }

        return null;
    }

    private async Task<UserMedication> EnsureUserMedicationForLogAsync(int profileId,int medicationId,string? notes) {
        var existing = await _userMedicationRepository
            .GetNewestActiveTrackedByProfileAndMedicationAsync(profileId,medicationId);
        if (existing is not null) {
            return existing;
        }

        var fallback = new UserMedication {
            ProfileId = profileId,
            MedicationId = medicationId,
            Frequency = 1,
            ScheduleUnit = MedicationScheduleUnit.Day,
            ScheduledTimesJson = MedicationConstants.DefaultScheduledTimeJson,
            WeeklyDaysJson = MedicationConstants.EmptyJsonArray,
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            RemindersEnabled = false,
            Notes = string.IsNullOrWhiteSpace(notes) ? MedicationConstants.Messages.TakeOnceFallbackNote : notes.Trim(),
            IsActive = false,
            AddedAt = DateTime.UtcNow
        };

        await _userMedicationRepository.AddAsync(fallback);
        await _userMedicationRepository.SaveChangesAsync();
        return fallback;
    }

    private void LogTakenDose(int userMedicationId) {
        var takenAt = DateTime.UtcNow;
        _doseLogRepository.Add(new DoseLog {
            UserMedicationId = userMedicationId,
            ScheduledTime = takenAt,
            TakenAt = takenAt,
            DoseStatus = DoseStatusEnum.Done
        });
    }

    private (int remaining,bool removed) ConsumeStock(HomePharmacyItem stockItem,int quantityToTake) {
        var remaining = stockItem.Quantity - quantityToTake;
        if (remaining <= 0) {
            _homePharmacyRepository.Remove(stockItem);
            return (0,true);
        }

        stockItem.Quantity = remaining;
        return (remaining,false);
    }

    private static (DateTime utcStart,DateTime utcEnd) GetUtcDateRange(DateOnly date) {
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue),DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        return (localStart.ToUniversalTime(),localEnd.ToUniversalTime());
    }

    private static int NormalizeQuantity(int quantity) => quantity <= 0 ? 1 : quantity;

    private static string? TrimNotesOrNull(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static TakeMedicationOnceResultDto Failure(string message) =>
        new() {
            Success = false,
            Message = message
        };

    private readonly record struct StockDecrementResult(
        int? RemainingQuantity,
        bool RemovedFromEverywhere,
        string? Warning) {
        public static StockDecrementResult NotApplicable => new(null,false,null);
    }
}
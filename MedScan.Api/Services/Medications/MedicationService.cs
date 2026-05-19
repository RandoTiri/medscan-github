using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Medications;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Services;

namespace MedScan.Api.Services.Medications;

public sealed class MedicationService(
    IUserMedicationRepository userMedicationRepository,
    IMedicationRepository medicationRepository,
    IDoseLogRepository doseLogRepository,
    DoseLogService doseLogService,
    MedicationStockService stockService,
    TakeMedicationOnceService takeMedicationOnceService) : IMedicationService {
    private readonly IUserMedicationRepository _userMedicationRepository = userMedicationRepository;
    private readonly IMedicationRepository _medicationRepository = medicationRepository;
    private readonly IDoseLogRepository _doseLogRepository = doseLogRepository;
    private readonly DoseLogService _doseLogService = doseLogService;
    private readonly MedicationStockService _stockService = stockService;
    private readonly TakeMedicationOnceService _takeMedicationOnceService = takeMedicationOnceService;

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

        var previousStatus = _doseLogService.UpsertScheduledDose(userMedication,dto.ScheduledTime,dto.Status);

        var stockResult = await _stockService.DecrementForCompletedDoseAsync(userMedication,dto.Status,previousStatus);

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

    public Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId,TakeMedicationOnceDto dto) =>
        _takeMedicationOnceService.TakeOnceAsync(medicationId,dto);

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

    private async Task<UserMedicationDto?> BuildUpdateStatusResultAsync(
        int userMedicationId,
        UserMedication userMedication,
        MedicationStockResult stockResult) {
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

    private static (DateTime utcStart,DateTime utcEnd) GetUtcDateRange(DateOnly date) {
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue),DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        return (localStart.ToUniversalTime(),localEnd.ToUniversalTime());
    }

    private static string? TrimNotesOrNull(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}

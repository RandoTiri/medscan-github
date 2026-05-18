using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;

namespace MedScan.Api.Services.Medications;

internal static class MedicationValidator {
    public static void EnsureValid(AddMedicationDto dto) {
        if (dto.ProfileId <= 0) {
            throw new InvalidOperationException(MedicationConstants.Messages.InvalidProfileId);
        }

        if (dto.MedicationId <= 0) {
            throw new InvalidOperationException(MedicationConstants.Messages.InvalidMedicationId);
        }

        if (dto.FrequencyPerDay < MedicationConstants.MinFrequencyPerDay ||
            dto.FrequencyPerDay > MedicationConstants.MaxFrequencyPerDay) {
            throw new InvalidOperationException(MedicationConstants.Messages.InvalidFrequency);
        }

        if (dto.ScheduledTimes is null || dto.ScheduledTimes.Count == 0) {
            throw new InvalidOperationException(MedicationConstants.Messages.ScheduledTimeRequired);
        }

        var expectedTimeCount = dto.ScheduleUnit == MedicationScheduleUnit.Month
            ? 1
            : dto.FrequencyPerDay;

        if (dto.ScheduledTimes.Count != expectedTimeCount) {
            throw new InvalidOperationException(MedicationConstants.Messages.ScheduledTimeCountMismatch);
        }

        if (dto.ScheduleUnit == MedicationScheduleUnit.Week && dto.FrequencyPerDay > 1) {
            EnsureValidWeeklyDays(dto.WeeklyDays,dto.FrequencyPerDay);
        }
    }

    public static List<int> NormalizeWeeklyDays(MedicationScheduleUnit unit,int frequency,List<int>? weeklyDays) {
        if (unit != MedicationScheduleUnit.Week || frequency <= 1) {
            return [];
        }

        return (weeklyDays ?? [])
            .Take(frequency)
            .ToList();
    }

    private static void EnsureValidWeeklyDays(List<int> weeklyDays,int frequency) {
        if (weeklyDays.Count != frequency) {
            throw new InvalidOperationException(MedicationConstants.Messages.WeeklyDaySelectionRequired);
        }

        if (weeklyDays.Any(day => day < MedicationConstants.MinWeeklyDay || day > MedicationConstants.MaxWeeklyDay)) {
            throw new InvalidOperationException(MedicationConstants.Messages.WeeklyDaysInvalid);
        }

        if (weeklyDays.Distinct().Count() != weeklyDays.Count) {
            throw new InvalidOperationException(MedicationConstants.Messages.WeeklyDaysMustBeDistinct);
        }
    }
}
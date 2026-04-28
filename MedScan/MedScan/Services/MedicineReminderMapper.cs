using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;

namespace MedScan.MAUI.Services;

public static class MedicineReminderMapper {
    public static MedicineReminderModel ToReminderModel(UserMedicationDto dto) {
        return new MedicineReminderModel {
            UserMedicationId = dto.Id,
            MedicationName = dto.MedicationName,
            ProfileName = dto.ProfileName,
            Dosage = dto.Strength ?? string.Empty,
            RemindersEnabled = dto.RemindersEnabled,
            Frequency = dto.FrequencyPerDay,
            ScheduleUnit = dto.ScheduleUnit,
            StartDate = dto.StartDate,
            ReminderTimes = dto.ScheduledTimes,
            WeeklyDays = dto.WeeklyDays
        };
    }
}

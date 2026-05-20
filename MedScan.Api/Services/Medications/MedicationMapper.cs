using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Utilities;

namespace MedScan.Api.Services.Medications;

internal static class MedicationMapper {
    public static UserMedicationDto ToDto(UserMedication userMedication,DateOnly? forDate = null) {
        var scheduledTimes = MedicationScheduleSerializer.DeserializeTimes(userMedication.ScheduledTimesJson);
        var weeklyDays = MedicationScheduleSerializer.DeserializeWeeklyDays(userMedication.WeeklyDaysJson);
        var doseStatuses = BuildDoseStatuses(userMedication,scheduledTimes,weeklyDays,forDate);
        var latestStatus = ResolveLatestStatus(userMedication);

        return new UserMedicationDto {
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

    private static DoseStatusEnum? ResolveLatestStatus(UserMedication userMedication) =>
        userMedication.DoseLogs
            .OrderByDescending(log => log.ScheduledTime)
            .ThenByDescending(log => log.Id)
            .Select(log => (DoseStatusEnum?)log.DoseStatus)
            .FirstOrDefault();

    private static List<ScheduledDoseStatusDto> BuildDoseStatuses(
        UserMedication userMedication,
        List<TimeOnly> scheduledTimes,
        List<int> weeklyDays,
        DateOnly? forDate) {
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

        if (occurrences.Count == 0) {
            return [];
        }

        var logsByScheduledUtc = userMedication.DoseLogs
            .Where(log => DateOnly.FromDateTime(log.ScheduledTime.ToLocalTime()) == selectedDate)
            .GroupBy(log => log.ScheduledTime)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(log => log.Id).First().DoseStatus);

        var result = new List<ScheduledDoseStatusDto>(occurrences.Count);

        foreach (var occurrence in occurrences) {
            var localScheduledDateTime = DateTime.SpecifyKind(
                selectedLocalDate.Add(occurrence.Time.ToTimeSpan()),
                DateTimeKind.Local);
            var scheduledUtc = localScheduledDateTime.ToUniversalTime();

            if (logsByScheduledUtc.TryGetValue(scheduledUtc,out var statusFromLog)) {
                result.Add(new ScheduledDoseStatusDto {
                    ScheduledTime = occurrence.Time,
                    Status = statusFromLog
                });
                continue;
            }

            var computedStatus = ResolvePendingStatus(selectedDate,localScheduledDateTime,nowLocal);
            result.Add(new ScheduledDoseStatusDto {
                ScheduledTime = occurrence.Time,
                Status = computedStatus
            });
        }

        return result;
    }

    private static DoseStatusEnum ResolvePendingStatus(DateOnly selectedDate,DateTime localScheduledDateTime,DateTime nowLocal) {
        if (selectedDate < DateOnly.FromDateTime(nowLocal)) {
            return DoseStatusEnum.Missed;
        }

        return localScheduledDateTime <= nowLocal
            ? DoseStatusEnum.Missed
            : DoseStatusEnum.Pending;
    }
}
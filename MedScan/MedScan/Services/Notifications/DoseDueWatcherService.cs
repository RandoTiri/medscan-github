using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services.Auth;
using MedScan.Shared.Services.Medications;
using MedScan.Shared.Services.Notifications;
using MedScan.Shared.Services.Profiles;
using Microsoft.Extensions.Logging;

namespace MedScan.MAUI.Services.Notifications;

public sealed class DoseDueWatcherService(
    IAuthService authService,
    IProfileService profileService,
    IMedicationService medicationService,
    IInAppDoseAlertService inAppDoseAlertService,
    ILogger<DoseDueWatcherService> logger) {
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DoseGracePeriod = TimeSpan.FromMinutes(5);

    private readonly HashSet<string> _firedKeys = [];
    private readonly object _startSync = new();
    private Task? _runner;

    public void EnsureStarted() {
        lock (_startSync) {
            _runner ??= Task.Run(RunLoopAsync);
        }
    }

    private async Task RunLoopAsync() {
        var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync()) {
            try {
                await authService.InitializeAsync();
                if (!authService.IsLoggedIn) {
                    continue;
                }

                await CheckDueDosesAsync();
            } catch (Exception ex) {
                logger.LogWarning(ex,"Dose due watcher poll failed.");
            }
        }
    }

    private async Task CheckDueDosesAsync() {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        _firedKeys.RemoveWhere(key => !key.StartsWith($"{today:yyyyMMdd}|",StringComparison.Ordinal));

        var nowTime = TimeOnly.FromDateTime(now);
        var profiles = await profileService.GetMyProfilesAsync();

        foreach (var profile in profiles) {
            var schedule = await medicationService.GetScheduleAsync(profile.Id);

            foreach (var medication in schedule.Where(m => m.IsActive && m.RemindersEnabled)) {
                foreach (var due in ResolveDueTimes(medication,nowTime)) {
                    var key = $"{today:yyyyMMdd}|{medication.Id}|{due:HHmm}";
                    if (_firedKeys.Contains(key)) 
                        continue;

                    _firedKeys.Add(key);
                    inAppDoseAlertService.Enqueue(new InAppDoseAlert {
                        UserMedicationId = medication.Id,
                        ScheduledTime = due,
                        MedicationName = BuildMedicationName(medication),
                        ProfileName = medication.ProfileName,
                        Note = string.IsNullOrWhiteSpace(medication.Notes) ? null : medication.Notes.Trim(),
                        TriggeredAt = now
                    });
                }
            }
        }
    }

    private static IEnumerable<TimeOnly> ResolveDueTimes(UserMedicationDto medication,TimeOnly now) {
        var gracePeriodStart = now.Add(-DoseGracePeriod);

        if (medication.TodayDoses.Count > 0) {
            return medication.TodayDoses
                .Where(dose => dose.Status is DoseStatusEnum.Pending or DoseStatusEnum.Missed)
                .Select(dose => dose.ScheduledTime)
                .Where(time => IsWithinGraceWindow(time,gracePeriodStart,now))
                .ToList();
        }

        if (medication.ScheduleUnit != MedicationScheduleUnit.Day) 
            return [];

        return medication.ScheduledTimes
            .Where(time => IsWithinGraceWindow(time,gracePeriodStart,now))
            .ToList();
    }

    private static bool IsWithinGraceWindow(TimeOnly value,TimeOnly startInclusive,TimeOnly endInclusive) =>
        value >= startInclusive && value <= endInclusive;

    private static string BuildMedicationName(UserMedicationDto medication) {
        if (string.IsNullOrWhiteSpace(medication.Strength) ||
            medication.MedicationName.Contains(medication.Strength,StringComparison.OrdinalIgnoreCase)) 
            return medication.MedicationName;

        return $"{medication.MedicationName} {medication.Strength}";
    }
}

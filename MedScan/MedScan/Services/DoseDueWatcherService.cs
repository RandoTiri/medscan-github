using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using System.Linq;

namespace MedScan.Services;

public sealed class DoseDueWatcherService(
    IAuthService authService,
    IProfileService profileService,
    IMedicationService medicationService,
    IInAppDoseAlertService inAppDoseAlertService)
{
    private readonly HashSet<string> _firedKeys = [];
    private readonly object _startSync = new();
    private Task? _runner;

    public void EnsureStarted()
    {
        lock (_startSync)
        {
            _runner ??= Task.Run(RunLoopAsync);
        }
    }

    private async Task RunLoopAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await authService.InitializeAsync();
                if (!authService.IsLoggedIn)
                {
                    continue;
                }

                await CheckDueDosesAsync();
            }
            catch
            {
                // Best effort watcher. Do not crash background loop.
            }
        }
    }

    private async Task CheckDueDosesAsync()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        _firedKeys.RemoveWhere(key => !key.StartsWith($"{today:yyyyMMdd}|", StringComparison.Ordinal));

        var nowTime = TimeOnly.FromDateTime(now);
        var profiles = await profileService.GetMyProfilesAsync();

        foreach (var profile in profiles)
        {
            var schedule = await medicationService.GetScheduleAsync(profile.Id);

            foreach (var medication in schedule.Where(m => m.IsActive && m.RemindersEnabled))
            {
                foreach (var due in ResolveDueTimes(medication, nowTime))
                {
                    var key = $"{today:yyyyMMdd}|{medication.Id}|{due:HHmm}";
                    if (_firedKeys.Contains(key))
                    {
                        continue;
                    }

                    _firedKeys.Add(key);
                    inAppDoseAlertService.Enqueue(new InAppDoseAlert
                    {
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

    private static IEnumerable<TimeOnly> ResolveDueTimes(UserMedicationDto medication, TimeOnly now)
    {
        var gracePeriodStart = now.Add(TimeSpan.FromMinutes(-5));

        if (medication.TodayDoses.Count > 0)
        {
            return medication.TodayDoses
                .Where(dose => dose.Status is DoseStatusEnum.Pending or DoseStatusEnum.Missed)
                .Select(dose => dose.ScheduledTime)
                .Where(time => IsWithinGraceWindow(time, gracePeriodStart, now))
                .ToList();
        }

        return medication.ScheduledTimes
            .Where(time => IsWithinGraceWindow(time, gracePeriodStart, now))
            .ToList();
    }

    private static bool IsWithinGraceWindow(TimeOnly value, TimeOnly startInclusive, TimeOnly endInclusive)
    {
        return value >= startInclusive && value <= endInclusive;
    }

    private static string BuildMedicationName(UserMedicationDto medication)
    {
        if (string.IsNullOrWhiteSpace(medication.Strength) ||
            medication.MedicationName.Contains(medication.Strength, StringComparison.OrdinalIgnoreCase))
        {
            return medication.MedicationName;
        }

        return $"{medication.MedicationName} {medication.Strength}";
    }
}

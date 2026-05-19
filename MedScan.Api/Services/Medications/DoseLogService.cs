using MedScan.Api.Repositories.DoseLogs;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;

namespace MedScan.Api.Services.Medications;

public sealed class DoseLogService(IDoseLogRepository doseLogRepository) {
    public DoseStatusEnum? UpsertScheduledDose(UserMedication userMedication,TimeOnly? scheduledTime,DoseStatusEnum status) {
        var scheduledUtc = ResolveScheduledUtc(scheduledTime);
        var existingLog = userMedication.DoseLogs
            .Where(log => log.ScheduledTime == scheduledUtc)
            .OrderByDescending(log => log.Id)
            .FirstOrDefault();

        var previousStatus = existingLog?.DoseStatus;
        var takenAt = status == DoseStatusEnum.Done ? DateTime.UtcNow : (DateTime?)null;

        if (existingLog is null) {
            doseLogRepository.Add(new DoseLog {
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

    public void AddTakenNow(int userMedicationId) {
        var takenAt = DateTime.UtcNow;
        doseLogRepository.Add(new DoseLog {
            UserMedicationId = userMedicationId,
            ScheduledTime = takenAt,
            TakenAt = takenAt,
            DoseStatus = DoseStatusEnum.Done
        });
    }

    private static DateTime ResolveScheduledUtc(TimeOnly? scheduledTime) {
        var nowLocal = DateTime.Now;
        var resolvedTime = scheduledTime ?? TimeOnly.FromDateTime(nowLocal);
        return DateTime
            .SpecifyKind(nowLocal.Date.Add(resolvedTime.ToTimeSpan()),DateTimeKind.Local)
            .ToUniversalTime();
    }
}
using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Services;

public interface IMedicationStatusEvents
{
    event Action<MedicationStatusChangedEvent>? Changed;
    void Publish(MedicationStatusChangedEvent payload);
}

public sealed record MedicationStatusChangedEvent(
    int UserMedicationId,
    TimeOnly ScheduledTime,
    DoseStatusEnum Status);

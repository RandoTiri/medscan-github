using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface IInAppDoseAlertService
{
    event Action? Changed;
    InAppDoseAlert? Current { get; }
    void Enqueue(InAppDoseAlert alert);
    void DismissCurrent();
    void DismissByDose(int userMedicationId, TimeOnly scheduledTime);
}

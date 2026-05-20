namespace MedScan.Shared.Services.Medications;

public sealed class MedicationStatusEvents : IMedicationStatusEvents
{
    public event Action<MedicationStatusChangedEvent>? Changed;

    public void Publish(MedicationStatusChangedEvent payload)
    {
        Changed?.Invoke(payload);
    }
}

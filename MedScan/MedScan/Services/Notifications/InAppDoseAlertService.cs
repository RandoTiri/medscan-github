using MedScan.Shared.Models;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services.Notifications;

public sealed class InAppDoseAlertService : IInAppDoseAlertService {
    private readonly Queue<InAppDoseAlert> _queue = new();
    private readonly object _sync = new();

    public event Action? Changed;
    public InAppDoseAlert? Current { get; private set; }

    public void Enqueue(InAppDoseAlert alert) {
        lock (_sync) {
            if (Current is not null && Matches(Current,alert.UserMedicationId,alert.ScheduledTime)) 
                return;

            if (_queue.Any(item => Matches(item,alert.UserMedicationId,alert.ScheduledTime))) 
                return;

            if (Current is null) {
                Current = alert;
            } else {
                _queue.Enqueue(alert);
            }
        }
        Changed?.Invoke();
    }

    public void DismissCurrent() {
        lock (_sync) {
            AdvanceCurrent();
        }
        Changed?.Invoke();
    }

    public void DismissByDose(int userMedicationId,TimeOnly scheduledTime) {
        var changed = false;

        lock (_sync) {
            if (Current is not null && Matches(Current,userMedicationId,scheduledTime)) {
                AdvanceCurrent();
                changed = true;
            }

            if (_queue.Count > 0) {
                var kept = _queue
                    .Where(item => !Matches(item,userMedicationId,scheduledTime))
                    .ToList();

                if (kept.Count != _queue.Count) {
                    _queue.Clear();
                    foreach (var item in kept) {
                        _queue.Enqueue(item);
                    }
                    changed = true;
                }
            }
        }

        if (changed) 
            Changed?.Invoke();
    }

    private void AdvanceCurrent() =>
        Current = _queue.Count > 0 ? _queue.Dequeue() : null;

    private static bool Matches(InAppDoseAlert alert,int userMedicationId,TimeOnly scheduledTime) =>
        alert.UserMedicationId == userMedicationId && alert.ScheduledTime == scheduledTime;
}
using MedScan.Shared.Models;
using MedScan.Shared.Services;
using System.Linq;

namespace MedScan.Services;

public sealed class InAppDoseAlertService : IInAppDoseAlertService
{
    private readonly Queue<InAppDoseAlert> _queue = new();
    private readonly object _sync = new();

    public event Action? Changed;
    public InAppDoseAlert? Current { get; private set; }

    public void Enqueue(InAppDoseAlert alert)
    {
        lock (_sync)
        {
            if (Current is not null &&
                Current.UserMedicationId == alert.UserMedicationId &&
                Current.ScheduledTime == alert.ScheduledTime)
            {
                return;
            }

            if (_queue.Any(item =>
                    item.UserMedicationId == alert.UserMedicationId &&
                    item.ScheduledTime == alert.ScheduledTime))
            {
                return;
            }

            if (Current is null)
            {
                Current = alert;
            }
            else
            {
                _queue.Enqueue(alert);
            }
        }

        Changed?.Invoke();
    }

    public void DismissCurrent()
    {
        lock (_sync)
        {
            if (_queue.Count > 0)
            {
                Current = _queue.Dequeue();
            }
            else
            {
                Current = null;
            }
        }

        Changed?.Invoke();
    }

    public void DismissByDose(int userMedicationId, TimeOnly scheduledTime)
    {
        var changed = false;

        lock (_sync)
        {
            if (Current is not null &&
                Current.UserMedicationId == userMedicationId &&
                Current.ScheduledTime == scheduledTime)
            {
                if (_queue.Count > 0)
                {
                    Current = _queue.Dequeue();
                }
                else
                {
                    Current = null;
                }

                changed = true;
            }

            if (_queue.Count > 0)
            {
                var kept = _queue
                    .Where(item => item.UserMedicationId != userMedicationId || item.ScheduledTime != scheduledTime)
                    .ToList();

                if (kept.Count != _queue.Count)
                {
                    _queue.Clear();
                    foreach (var item in kept)
                    {
                        _queue.Enqueue(item);
                    }

                    changed = true;
                }
            }
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }
}

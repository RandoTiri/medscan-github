using MedScan.Shared.Models;

namespace MedScan.Shared.Services;

public interface INotificationInboxService
{
    event Action? Changed;
    Task InitializeAsync();
    Task<IReadOnlyList<NotificationInboxItem>> GetHistoryAsync();
    Task<bool> HasUnrespondedAsync();
}

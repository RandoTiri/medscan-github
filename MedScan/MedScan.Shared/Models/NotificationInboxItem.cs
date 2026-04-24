namespace MedScan.Shared.Models;

public sealed class NotificationInboxItem
{
    public int NotificationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public bool IsResponded { get; init; }
    public string? ResponseLabel { get; init; }
}

using MedScan.Shared.Models;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.EventArgs;
using System.Text.Json;

namespace MedScan.MAUI.Services.Notifications;

public sealed class LocalNotificationInboxService(
    IServiceProvider serviceProvider,
    IInAppDoseAlertService inAppDoseAlertService,
    ILogger<LocalNotificationInboxService> logger) : INotificationInboxService {
    private const string StorageKey = "medscan.notification.inbox.v1";
    private const int HistoryLimit = 100;
    private const int RecordRetentionLimit = 200;
    private static readonly TimeSpan DuplicateDetectionWindow = TimeSpan.FromMinutes(1);

    private const string DefaultProfileName = "Mina";
    private const string ActionLabelDone = "Võetud";
    private const string ActionLabelOpened = "Avatud";
    private const string ActionLabelDismissed = "Suletud";

    private readonly SemaphoreSlim _sync = new(1,1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private bool _isInitialized;
    private List<NotificationInboxRecord> _records = [];

    public event Action? Changed;

    public async Task InitializeAsync() {
        if (_isInitialized) 
            return;

        await _sync.WaitAsync();
        try {
            if (_isInitialized) 
                return;

            _records = LoadRecords();
            LocalNotificationCenter.Current.NotificationReceived += OnNotificationReceived;
            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;

            await RefreshFromDeliveredAsync();
            _isInitialized = true;
        } finally {
            _sync.Release();
        }
    }

    public async Task<IReadOnlyList<NotificationInboxItem>> GetHistoryAsync() {
        await InitializeAsync();
        await _sync.WaitAsync();
        try {
            await RefreshFromDeliveredAsync();
            return _records
                .OrderByDescending(r => r.OccurredAtUtc)
                .Take(HistoryLimit)
                .Select(MapToItem)
                .ToList();
        } finally {
            _sync.Release();
        }
    }

    public async Task<bool> HasUnrespondedAsync() {
        await InitializeAsync();
        await _sync.WaitAsync();
        try {
            await RefreshFromDeliveredAsync();
            return _records.Any(record => !record.IsResponded);
        } finally {
            _sync.Release();
        }
    }

    private void OnNotificationReceived(NotificationEventArgs e) {
        _ = HandleReceivedAsync(e.Request);
    }

    private void OnNotificationActionTapped(NotificationActionEventArgs e) {
        _ = HandleActionAsync(e);
    }

    private async Task HandleReceivedAsync(NotificationRequest? request) {
        if (request is null) 
            return;

        await _sync.WaitAsync();
        try {
            UpsertRecord(request,DateTime.UtcNow);
            SaveRecords();
        } finally {
            _sync.Release();
        }

        await TryEnqueueInAppAlertAsync(request);
        Changed?.Invoke();
    }

    private async Task HandleActionAsync(NotificationActionEventArgs e) {
        var request = GetRequestFromEvent(e);
        if (request is null) 
            return;

        await _sync.WaitAsync();
        try {
            UpsertRecord(request,DateTime.UtcNow);

            var record = _records
                .Where(r => r.NotificationId == request.NotificationId)
                .OrderByDescending(r => r.OccurredAtUtc)
                .FirstOrDefault();

            if (record is not null) {
                if (TryResolveActionLabel(e.ActionId,out var actionLabel)) {
                    record.IsResponded = true;
                    record.ResponseLabel = actionLabel;
                } else if (e.IsTapped || e.IsDismissed) {
                    record.IsResponded = true;
                    record.ResponseLabel = e.IsTapped ? ActionLabelOpened : ActionLabelDismissed;
                }
            }
            SaveRecords();
        } finally {
            _sync.Release();
        }
        Changed?.Invoke();
    }

    private async Task RefreshFromDeliveredAsync() {
        var delivered = await LocalNotificationCenter.Current.GetDeliveredNotificationList();
        var hasNewRecords = false;

        foreach (var request in delivered ?? []) {
            var occurredAtUtc = request.Schedule?.NotifyTime?.UtcDateTime ?? DateTime.UtcNow;
            if (UpsertRecord(request,occurredAtUtc)) {
                hasNewRecords = true;
            }
        }

        if (hasNewRecords) 
            SaveRecords();
    }

    private bool UpsertRecord(NotificationRequest request,DateTime occurredAtUtc) {
        var title = request.Title?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var exists = _records.Any(record =>
            record.NotificationId == request.NotificationId &&
            record.Title == title &&
            record.Description == description &&
            Math.Abs((record.OccurredAtUtc - occurredAtUtc).TotalMinutes) < DuplicateDetectionWindow.TotalMinutes);

        if (exists) 
            return false;

        _records.Add(new NotificationInboxRecord {
            NotificationId = request.NotificationId,
            Title = title,
            Description = description,
            OccurredAtUtc = occurredAtUtc,
            IsResponded = false
        });
        return true;
    }

    private static NotificationRequest? GetRequestFromEvent(NotificationActionEventArgs e) {
        var prop = e.GetType().GetProperty("Request");
        return prop?.GetValue(e) as NotificationRequest;
    }

    private static bool TryResolveActionLabel(int actionId,out string label) {
        label = actionId switch {
            MauiMedicineReminderScheduler.DoneActionId => ActionLabelDone,
            _ => string.Empty
        };
        return !string.IsNullOrWhiteSpace(label);
    }

    private async Task TryEnqueueInAppAlertAsync(NotificationRequest request) {
        if (!ReminderPayloadCodec.TryDecode(request.ReturningData,out var payload)) 
            return;

        if (!string.IsNullOrWhiteSpace(payload.MedicationName)) {
            EnqueueFromPayload(payload);
            return;
        }
        await EnqueueByFetchingMedicationAsync(payload);
    }

    private void EnqueueFromPayload(DecodedReminderPayload payload) {
        inAppDoseAlertService.Enqueue(new InAppDoseAlert {
            UserMedicationId = payload.UserMedicationId,
            ScheduledTime = payload.ScheduledTime,
            MedicationName = payload.MedicationName!,
            ProfileName = string.IsNullOrWhiteSpace(payload.ProfileName) ? DefaultProfileName : payload.ProfileName!,
            Note = payload.Note,
            TriggeredAt = DateTime.Now
        });
    }

    private async Task EnqueueByFetchingMedicationAsync(DecodedReminderPayload payload) {
        try {
            using var scope = serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var medicationService = scope.ServiceProvider.GetRequiredService<IMedicationService>();

            await authService.InitializeAsync();

            var profileId = authService.CurrentUser?.DefaultProfileId;
            if (!profileId.HasValue) 
                return;

            var medication = (await medicationService.GetScheduleAsync(profileId.Value))
                .FirstOrDefault(item => item.Id == payload.UserMedicationId);

            if (medication is null) 
                return;

            inAppDoseAlertService.Enqueue(new InAppDoseAlert {
                UserMedicationId = payload.UserMedicationId,
                ScheduledTime = payload.ScheduledTime,
                MedicationName = medication.MedicationName,
                ProfileName = medication.ProfileName,
                Note = string.IsNullOrWhiteSpace(medication.Notes) ? null : medication.Notes.Trim(),
                TriggeredAt = DateTime.Now
            });
        } catch (Exception ex) {
            logger.LogWarning(ex,"Failed to enqueue in-app alert from notification payload.");
        }
    }

    private List<NotificationInboxRecord> LoadRecords() {
        try {
            var raw = Preferences.Default.Get(StorageKey,string.Empty);
            if (string.IsNullOrWhiteSpace(raw)) 
                return [];

            var records = JsonSerializer.Deserialize<List<NotificationInboxRecord>>(raw,_jsonOptions);
            return records ?? [];
        } catch (JsonException ex) {
            logger.LogWarning(ex,"Failed to read notification inbox records from local preferences.");
            return [];
        }
    }

    private void SaveRecords() {
        var trimmed = _records
            .OrderByDescending(r => r.OccurredAtUtc)
            .Take(RecordRetentionLimit)
            .ToList();

        _records = trimmed;

        var raw = JsonSerializer.Serialize(trimmed,_jsonOptions);
        Preferences.Default.Set(StorageKey,raw);
    }

    private static NotificationInboxItem MapToItem(NotificationInboxRecord record) {
        return new NotificationInboxItem {
            NotificationId = record.NotificationId,
            Title = record.Title,
            Description = record.Description,
            OccurredAt = record.OccurredAtUtc.ToLocalTime(),
            IsResponded = record.IsResponded,
            ResponseLabel = record.ResponseLabel
        };
    }

    private sealed class NotificationInboxRecord {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public bool IsResponded { get; set; }
        public string? ResponseLabel { get; set; }
    }
}

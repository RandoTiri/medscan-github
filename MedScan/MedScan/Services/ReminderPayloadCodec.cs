using System.Text.Json;

namespace MedScan.MAUI.Services;

public static class ReminderPayloadCodec
{
    private sealed class ReminderPayload
    {
        public int UserMedicationId { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public string? MedicationName { get; set; }
        public string? ProfileName { get; set; }
        public string? Note { get; set; }
    }

    public static string Encode(
        int userMedicationId,
        TimeOnly scheduledTime,
        string? medicationName = null,
        string? profileName = null,
        string? note = null)
    {
        return JsonSerializer.Serialize(new ReminderPayload
        {
            UserMedicationId = userMedicationId,
            Hour = scheduledTime.Hour,
            Minute = scheduledTime.Minute,
            MedicationName = TrimOrNull(medicationName),
            ProfileName = TrimOrNull(profileName),
            Note = TrimOrNull(note)
        });
    }

    public static bool TryDecode(string? payload, out int userMedicationId, out TimeOnly scheduledTime)
    {
        var success = TryDecode(payload, out var decoded);
        userMedicationId = decoded.UserMedicationId;
        scheduledTime = decoded.ScheduledTime;
        return success;
    }

    public static bool TryDecode(string? payload, out DecodedReminderPayload decoded)
    {
        decoded = new DecodedReminderPayload();

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ReminderPayload>(payload);
            if (parsed is null || parsed.UserMedicationId <= 0)
            {
                return false;
            }

            decoded = new DecodedReminderPayload
            {
                UserMedicationId = parsed.UserMedicationId,
                ScheduledTime = new TimeOnly(parsed.Hour, parsed.Minute),
                MedicationName = TrimOrNull(parsed.MedicationName),
                ProfileName = TrimOrNull(parsed.ProfileName),
                Note = TrimOrNull(parsed.Note)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

public sealed class DecodedReminderPayload
{
    public int UserMedicationId { get; init; }
    public TimeOnly ScheduledTime { get; init; }
    public string? MedicationName { get; init; }
    public string? ProfileName { get; init; }
    public string? Note { get; init; }
}

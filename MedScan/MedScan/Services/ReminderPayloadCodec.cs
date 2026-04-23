using System.Text.Json;

namespace MedScan.MAUI.Services;

public static class ReminderPayloadCodec
{
    private sealed class ReminderPayload
    {
        public int UserMedicationId { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
    }

    public static string Encode(int userMedicationId, TimeOnly scheduledTime)
    {
        return JsonSerializer.Serialize(new ReminderPayload
        {
            UserMedicationId = userMedicationId,
            Hour = scheduledTime.Hour,
            Minute = scheduledTime.Minute
        });
    }

    public static bool TryDecode(string? payload, out int userMedicationId, out TimeOnly scheduledTime)
    {
        userMedicationId = 0;
        scheduledTime = default;

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

            userMedicationId = parsed.UserMedicationId;
            scheduledTime = new TimeOnly(parsed.Hour, parsed.Minute);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

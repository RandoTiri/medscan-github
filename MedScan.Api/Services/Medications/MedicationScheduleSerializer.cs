using System.Text.Json;

namespace MedScan.Api.Services.Medications;

public static class MedicationScheduleSerializer {
    public static string SerializeTimes(IEnumerable<TimeOnly> times) =>
        JsonSerializer.Serialize(times);

    public static string SerializeWeeklyDays(IEnumerable<int> weeklyDays) =>
        JsonSerializer.Serialize(weeklyDays);

    public static List<TimeOnly> DeserializeTimes(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return [];
        }

        return JsonSerializer.Deserialize<List<TimeOnly>>(json) ?? [];
    }

    public static List<int> DeserializeWeeklyDays(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return [];
        }

        return JsonSerializer.Deserialize<List<int>>(json) ?? [];
    }
}
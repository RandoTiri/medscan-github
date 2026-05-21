using System.Text.Json;

namespace MedScan.Shared.Services.Common;

public static class ApiErrorReader {
    private const string DefaultErrorMessage = "Toiming ebaõnnestus.";

    public static async Task<string> ReadAsync(HttpResponseMessage response) {
        var raw = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(raw)) {
            return DefaultErrorMessage;
        }

        try {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            return root.ValueKind switch {
                JsonValueKind.Object => ReadObjectMessage(root),
                JsonValueKind.Array => ReadArrayMessage(root),
                _ => raw
            };
        } catch (JsonException) {
            return raw;
        }
    }

    private static string ReadObjectMessage(JsonElement root) {
        if (root.TryGetProperty("message",out var messageElement) &&
            messageElement.ValueKind == JsonValueKind.String) {
            return messageElement.GetString() ?? DefaultErrorMessage;
        }

        if (root.TryGetProperty("title",out var titleElement) &&
            titleElement.ValueKind == JsonValueKind.String) {
            return titleElement.GetString() ?? DefaultErrorMessage;
        }

        return DefaultErrorMessage;
    }

    private static string ReadArrayMessage(JsonElement root) {
        var messages = root.EnumerateArray()
            .Select(ReadArrayItemMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();

        var combined = string.Join(" ",messages);
        return string.IsNullOrWhiteSpace(combined) ? DefaultErrorMessage : combined;
    }

    private static string? ReadArrayItemMessage(JsonElement item) {
        if (item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty("description",out var descriptionElement) &&
            descriptionElement.ValueKind == JsonValueKind.String) {
            return descriptionElement.GetString();
        }

        return item.ToString();
    }
}
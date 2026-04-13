using System.Net.Http;
using System.Net.Http.Json;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Services;

namespace MedScan.MAUI.Services;

public sealed class ApiMedicationService : IMedicationService {
    private readonly HttpClient _httpClient;
    private readonly MedicineReminderCoordinator _reminderCoordinator;

    public ApiMedicationService(
        HttpClient httpClient,
        MedicineReminderCoordinator reminderCoordinator) {
        _httpClient = httpClient;
        _reminderCoordinator = reminderCoordinator;
    }

    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId) {
        var medications = await _httpClient.GetFromJsonAsync<List<UserMedicationDto>>(
            $"api/medications?profileId={profileId}");

        return medications ?? [];
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId) {
        return await _httpClient.GetFromJsonAsync<UserMedicationDto>(
            $"api/medications/{userMedicationId}");
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto) {
        var response = await _httpClient.PostAsJsonAsync("api/medications",dto);
        response.EnsureSuccessStatusCode();

        var savedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        await TryScheduleAsync(savedMedication);

        return savedMedication;
    }

    public async Task<UserMedicationDto> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto) {
        var existingMedication = await GetByIdAsync(userMedicationId);

        var response = await _httpClient.PutAsJsonAsync($"api/medications/{userMedicationId}",dto);
        response.EnsureSuccessStatusCode();

        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        if (existingMedication is not null) {
            await TryCancelAsync(existingMedication);
        }

        await TryScheduleAsync(updatedMedication);

        return updatedMedication;
    }

    public async Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto) {
        var response = await _httpClient.PostAsJsonAsync($"api/medications/{userMedicationId}/status", dto);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserMedicationDto>();
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId) {
        var existingMedication = await GetByIdAsync(userMedicationId);

        var response = await _httpClient.DeleteAsync($"api/medications/{userMedicationId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return false;
        }

        response.EnsureSuccessStatusCode();

        if (existingMedication is not null) {
            await TryCancelAsync(existingMedication);
        }

        return true;
    }

    private async Task TryScheduleAsync(UserMedicationDto medication) {
        try {
            await _reminderCoordinator.ScheduleForMedicineAsync(medication);
        }
        catch {
            // Medication save should not fail due to local notification issues.
        }
    }

    private async Task TryCancelAsync(UserMedicationDto medication) {
        try {
            await _reminderCoordinator.CancelForMedicineAsync(medication);
        }
        catch {
            // Medication update/delete should not fail due to local notification issues.
        }
    }
}

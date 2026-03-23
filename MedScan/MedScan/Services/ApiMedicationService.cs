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

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto) {
        var response = await _httpClient.PostAsJsonAsync("api/medications",dto);
        response.EnsureSuccessStatusCode();

        var savedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        await _reminderCoordinator.ScheduleForMedicineAsync(savedMedication);

        return savedMedication;
    }

    public async Task<UserMedicationDto> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto) {
        var existingMedication = await GetMedicationByIdAsync(userMedicationId);
        if (existingMedication is not null) {
            await _reminderCoordinator.CancelForMedicineAsync(existingMedication);
        }

        var response = await _httpClient.PutAsJsonAsync($"api/medications/{userMedicationId}",dto);
        response.EnsureSuccessStatusCode();

        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        await _reminderCoordinator.ScheduleForMedicineAsync(updatedMedication);

        return updatedMedication;
    }

    public async Task RemoveFromScheduleAsync(int userMedicationId) {
        var existingMedication = await GetMedicationByIdAsync(userMedicationId);
        if (existingMedication is not null) {
            await _reminderCoordinator.CancelForMedicineAsync(existingMedication);
        }

        var response = await _httpClient.DeleteAsync($"api/medications/{userMedicationId}");
        response.EnsureSuccessStatusCode();
    }

    private async Task<UserMedicationDto?> GetMedicationByIdAsync(int userMedicationId) {
        return await _httpClient.GetFromJsonAsync<UserMedicationDto>($"api/medications/{userMedicationId}");
    }
}
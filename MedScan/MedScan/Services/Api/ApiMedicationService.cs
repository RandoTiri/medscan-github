using MedScan.MAUI.Services.Notifications;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace MedScan.MAUI.Services.Api;

public sealed class ApiMedicationService(
    HttpClient httpClient,
    MedicineReminderCoordinator reminderCoordinator,
    IMedicationStatusEvents medicationStatusEvents,
    ILogger<ApiMedicationService> logger) : IMedicationService {
    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId,DateOnly? forDate = null) {
        var route = $"api/medications?profileId={profileId}";
        if (forDate is DateOnly selectedDate) {
            route += $"&forDate={selectedDate:yyyy-MM-dd}";
        }

        var medications = await httpClient.GetFromJsonAsync<List<UserMedicationDto>>(route);
        return medications ?? [];
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId) {
        using var response = await httpClient.GetAsync($"api/medications/{userMedicationId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserMedicationDto>();
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto) {
        using var response = await httpClient.PostAsJsonAsync("api/medications",dto);
        response.EnsureSuccessStatusCode();

        var savedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        await TryScheduleAsync(savedMedication);
        return savedMedication;
    }

    public async Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId,DateOnly date) {
        var route = $"api/medications/history?profileId={profileId}&date={date:yyyy-MM-dd}";
        var history = await httpClient.GetFromJsonAsync<List<DoseHistoryItemDto>>(route);
        return history ?? [];
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId,AddMedicationDto dto) {
        var existingMedication = await GetByIdAsync(userMedicationId);

        using var response = await httpClient.PutAsJsonAsync($"api/medications/{userMedicationId}",dto);
        response.EnsureSuccessStatusCode();

        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        if (existingMedication is not null) 
            await TryCancelAsync(existingMedication);
        await TryScheduleAsync(updatedMedication);

        return updatedMedication;
    }

    public async Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId,UpdateMedicationStatusDto dto) {
        using var response = await httpClient.PostAsJsonAsync($"api/medications/{userMedicationId}/status",dto);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>();

        if (updatedMedication is not null && dto.ScheduledTime.HasValue) {
            medicationStatusEvents.Publish(new MedicationStatusChangedEvent(
                userMedicationId,
                dto.ScheduledTime.Value,
                dto.Status));

            await TryAdjustReminderAfterStatusChangeAsync(updatedMedication,dto.ScheduledTime.Value,dto.Status);
        }

        return updatedMedication;
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId) {
        var existingMedication = await GetByIdAsync(userMedicationId);

        using var response = await httpClient.DeleteAsync($"api/medications/{userMedicationId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        response.EnsureSuccessStatusCode();

        if (existingMedication is not null) 
            await TryCancelAsync(existingMedication);

        return true;
    }

    public async Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId,TakeMedicationOnceDto dto) {
        using var response = await httpClient.PostAsJsonAsync($"api/medications/{medicationId}/take-once",dto);
        var result = await response.Content.ReadFromJsonAsync<TakeMedicationOnceResultDto>();

        if (response.IsSuccessStatusCode && result is not null) return result;

        if (response.StatusCode == HttpStatusCode.Conflict && result is not null) return result;

        response.EnsureSuccessStatusCode();
        return result ?? new TakeMedicationOnceResultDto {
            Success = false,
            Message = "Ühekordse võtmise salvestamine ebaõnnestus."
        };
    }

    private async Task TryScheduleAsync(UserMedicationDto medication) {
        try {
            await reminderCoordinator.ScheduleForMedicineAsync(medication);
        } catch (Exception ex) {
            logger.LogWarning(ex,"Failed to schedule reminders for medication {UserMedicationId}.",medication.Id);
        }
    }

    private async Task TryCancelAsync(UserMedicationDto medication) {
        try {
            await reminderCoordinator.CancelForMedicineAsync(medication);
        } catch (Exception ex) {
            logger.LogWarning(ex,"Failed to cancel reminders for medication {UserMedicationId}.",medication.Id);
        }
    }

    private async Task TryAdjustReminderAfterStatusChangeAsync(UserMedicationDto medication,TimeOnly scheduledTime,DoseStatusEnum status) {
        try {
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (status == DoseStatusEnum.Done && scheduledTime > now) {
                await reminderCoordinator.SkipTodayDoseAsync(medication,scheduledTime);
                return;
            }

            if (status == DoseStatusEnum.Pending && scheduledTime > now) {
                await reminderCoordinator.EnsureDoseFromNowAsync(medication,scheduledTime);
            }
        } catch (Exception ex) {
            logger.LogWarning(ex,"Failed to adjust reminder after status change for medication {UserMedicationId}.",medication.Id);
        }
    }
}

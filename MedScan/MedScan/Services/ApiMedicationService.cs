using System.Net.Http;
using System.Net.Http.Json;
using MedScan.MAUI.Services;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.Services;

public sealed class ApiMedicationService(
    HttpClient httpClient,
    MedicineReminderCoordinator reminderCoordinator,
    IMedicationStatusEvents medicationStatusEvents) : IMedicationService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly MedicineReminderCoordinator _reminderCoordinator = reminderCoordinator;
    private readonly IMedicationStatusEvents _medicationStatusEvents = medicationStatusEvents;

    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId, DateOnly? forDate = null)
    {
        var route = $"api/medications?profileId={profileId}";
        if (forDate is DateOnly selectedDate)
        {
            route += $"&forDate={selectedDate:yyyy-MM-dd}";
        }

        var medications = await _httpClient.GetFromJsonAsync<List<UserMedicationDto>>(
            route);

        return medications ?? [];
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId)
    {
        return await _httpClient.GetFromJsonAsync<UserMedicationDto>(
            $"api/medications/{userMedicationId}");
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/medications", dto);
        response.EnsureSuccessStatusCode();

        var savedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        await TryScheduleAsync(savedMedication);

        return savedMedication;
    }

    public async Task<IEnumerable<DoseHistoryItemDto>> GetHistoryAsync(int profileId, DateOnly date)
    {
        var route = $"api/medications/history?profileId={profileId}&date={date:yyyy-MM-dd}";
        var history = await _httpClient.GetFromJsonAsync<List<DoseHistoryItemDto>>(route);
        return history ?? [];
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId, AddMedicationDto dto)
    {
        var existingMedication = await GetByIdAsync(userMedicationId);

        var response = await _httpClient.PutAsJsonAsync($"api/medications/{userMedicationId}", dto);
        response.EnsureSuccessStatusCode();

        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>()
            ?? throw new InvalidOperationException("Medication response was empty.");

        if (existingMedication is not null)
        {
            await TryCancelAsync(existingMedication);
        }

        await TryScheduleAsync(updatedMedication);

        return updatedMedication;
    }

    public async Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/medications/{userMedicationId}/status", dto);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var updatedMedication = await response.Content.ReadFromJsonAsync<UserMedicationDto>();

        if (updatedMedication is not null && dto.ScheduledTime.HasValue)
        {
            _medicationStatusEvents.Publish(new MedicationStatusChangedEvent(
                userMedicationId,
                dto.ScheduledTime.Value,
                dto.Status));

            await TryAdjustReminderAfterStatusChangeAsync(updatedMedication, dto.ScheduledTime.Value, dto.Status);
        }

        return updatedMedication;
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId)
    {
        var existingMedication = await GetByIdAsync(userMedicationId);

        var response = await _httpClient.DeleteAsync($"api/medications/{userMedicationId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        if (existingMedication is not null)
        {
            await TryCancelAsync(existingMedication);
        }

        return true;
    }

    public async Task<TakeMedicationOnceResultDto> TakeOnceAsync(int medicationId, TakeMedicationOnceDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/medications/{medicationId}/take-once", dto);
        var result = await response.Content.ReadFromJsonAsync<TakeMedicationOnceResultDto>();

        if (response.IsSuccessStatusCode && result is not null)
        {
            return result;
        }

        if ((int)response.StatusCode == 409 && result is not null)
        {
            return result;
        }

        response.EnsureSuccessStatusCode();
        return result ?? new TakeMedicationOnceResultDto
        {
            Success = false,
            Message = "Ühekordse võtmise salvestamine ebaõnnestus."
        };
    }

    private async Task TryScheduleAsync(UserMedicationDto medication)
    {
        try
        {
            await _reminderCoordinator.ScheduleForMedicineAsync(medication);
        }
        catch
        {
            // Medication save should not fail due to local notification issues.
        }
    }

    private async Task TryCancelAsync(UserMedicationDto medication)
    {
        try
        {
            await _reminderCoordinator.CancelForMedicineAsync(medication);
        }
        catch
        {
            // Medication update/delete should not fail due to local notification issues.
        }
    }

    private async Task TryAdjustReminderAfterStatusChangeAsync(UserMedicationDto medication, TimeOnly scheduledTime, DoseStatusEnum status)
    {
        try
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);

            if (status == DoseStatusEnum.Done && scheduledTime > now)
            {
                await _reminderCoordinator.SkipTodayDoseAsync(medication, scheduledTime);
                return;
            }

            if (status == DoseStatusEnum.Pending && scheduledTime > now)
            {
                await _reminderCoordinator.EnsureDoseFromNowAsync(medication, scheduledTime);
            }
        }
        catch
        {
            // Dose status update should not fail due to local notification issues.
        }
    }
}

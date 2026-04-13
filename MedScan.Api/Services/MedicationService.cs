using System.Text.Json;
using MedScan.Api.Repositories;
using MedScan.Shared.DTOs.Medication;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;

namespace MedScan.Api.Services;

public sealed class MedicationService : IMedicationService
{
    private readonly IUserMedicationRepository _userMedicationRepository;
    private readonly IMedicationRepository _medicationRepository;

    public MedicationService(
        IUserMedicationRepository userMedicationRepository,
        IMedicationRepository medicationRepository)
    {
        _userMedicationRepository = userMedicationRepository;
        _medicationRepository = medicationRepository;
    }

    public async Task<IEnumerable<UserMedicationDto>> GetScheduleAsync(int profileId)
    {
        var items = await _userMedicationRepository.GetByProfileIdAsync(profileId);
        return items.Select(MapToDto);
    }

    public async Task<UserMedicationDto?> GetByIdAsync(int userMedicationId)
    {
        var item = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        return item is null ? null : MapToDto(item);
    }

    public async Task<UserMedicationDto> AddToScheduleAsync(AddMedicationDto dto)
    {
        var medication = await _medicationRepository.FindByIdAsync(dto.MedicationId);
        if (medication is null)
        {
            throw new InvalidOperationException($"Medication with id {dto.MedicationId} was not found.");
        }

        var userMedication = new UserMedication
        {
            ProfileId = dto.ProfileId,
            MedicationId = dto.MedicationId,
            Frequency = dto.FrequencyPerDay,
            ScheduledTimesJson = SerializeTimes(dto.ScheduledTimes),
            RemindersEnabled = dto.RemindersEnabled,
            Notes = dto.Notes,
            IsActive = true,
            AddedAt = DateTime.UtcNow
        };

        await _userMedicationRepository.AddAsync(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        var created = await _userMedicationRepository.GetByIdAsync(userMedication.Id)
            ?? throw new InvalidOperationException("Created medication could not be reloaded.");

        return MapToDto(created);
    }

    public async Task<UserMedicationDto?> UpdateScheduleAsync(int userMedicationId, AddMedicationDto dto)
    {
        var userMedication = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return null;
        }

        userMedication.ProfileId = dto.ProfileId;
        userMedication.MedicationId = dto.MedicationId;
        userMedication.Frequency = dto.FrequencyPerDay;
        userMedication.ScheduledTimesJson = SerializeTimes(dto.ScheduledTimes);
        userMedication.RemindersEnabled = dto.RemindersEnabled;
        userMedication.Notes = dto.Notes;

        await _userMedicationRepository.SaveChangesAsync();

        var updated = await _userMedicationRepository.GetByIdAsync(userMedicationId)
            ?? throw new InvalidOperationException("Updated medication could not be reloaded.");

        return MapToDto(updated);
    }

    public async Task<bool> RemoveFromScheduleAsync(int userMedicationId)
    {
        var userMedication = await _userMedicationRepository.GetByIdAsync(userMedicationId);
        if (userMedication is null)
        {
            return false;
        }

        _userMedicationRepository.Remove(userMedication);
        await _userMedicationRepository.SaveChangesAsync();

        return true;
    }

    public Task<UserMedicationDto?> UpdateStatusAsync(int userMedicationId, UpdateMedicationStatusDto dto)
    {
        throw new NotSupportedException("Status updates are handled by MedicationsController.");
    }

    private static UserMedicationDto MapToDto(UserMedication userMedication)
    {
        var latestStatus = userMedication.DoseLogs
            .OrderByDescending(log => log.ScheduledTime)
            .ThenByDescending(log => log.Id)
            .Select(log => (DoseStatusEnum?)log.DoseStatus)
            .FirstOrDefault();

        return new UserMedicationDto
        {
            Id = userMedication.Id,
            ProfileId = userMedication.ProfileId,
            ProfileName = userMedication.Profile?.Name ?? string.Empty,
            MedicationId = userMedication.MedicationId,
            MedicationName = userMedication.Medication?.Name ?? string.Empty,
            Strength = userMedication.Medication?.StrengthMg is int mg
                ? $"{mg} mg"
                : null,
            FrequencyPerDay = userMedication.Frequency,
            ScheduledTimes = DeserializeTimes(userMedication.ScheduledTimesJson),
            RemindersEnabled = userMedication.RemindersEnabled,
            Notes = userMedication.Notes,
            IsActive = userMedication.IsActive,
            LatestDoseStatus = latestStatus
        };
    }

    private static string SerializeTimes(List<TimeOnly> times)
    {
        return JsonSerializer.Serialize(times);
    }

    private static List<TimeOnly> DeserializeTimes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TimeOnly>>(json) ?? [];
    }
}

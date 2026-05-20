using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Models;


public readonly record struct BirthDateBuildResult(
    bool Success,
    DateOnly? BirthDate,
    BirthDateBuildStatus Status);

public static class BirthDateRules {
    public const int MinimumYear = 1900;

    public static BirthDateBuildResult BuildFromParts(
        string? day,
        string? month,
        string? year,
        bool isRequired) {
        var hasAny = !string.IsNullOrWhiteSpace(day) ||
                     !string.IsNullOrWhiteSpace(month) ||
                     !string.IsNullOrWhiteSpace(year);

        var hasAll = !string.IsNullOrWhiteSpace(day) &&
                     !string.IsNullOrWhiteSpace(month) &&
                     !string.IsNullOrWhiteSpace(year);

        if (!hasAny) {
            return isRequired
                ? new BirthDateBuildResult(false,null,BirthDateBuildStatus.Empty)
                : new BirthDateBuildResult(true,null,BirthDateBuildStatus.Empty);
        }

        if (!hasAll) {
            return new BirthDateBuildResult(false,null,BirthDateBuildStatus.Incomplete);
        }

        if (!int.TryParse(year,out var parsedYear) ||
            !int.TryParse(month,out var parsedMonth) ||
            !int.TryParse(day,out var parsedDay)) {
            return new BirthDateBuildResult(false,null,BirthDateBuildStatus.InvalidValue);
        }

        if (parsedYear < MinimumYear || parsedYear > AppDate.CurrentYear) {
            return new BirthDateBuildResult(false,null,BirthDateBuildStatus.OutOfRange);
        }

        try {
            var birthDate = new DateOnly(parsedYear,parsedMonth,parsedDay);
            return birthDate > AppDate.Today
                ? new BirthDateBuildResult(false,null,BirthDateBuildStatus.FutureDate)
                : new BirthDateBuildResult(true,birthDate,BirthDateBuildStatus.Valid);
        } catch (ArgumentOutOfRangeException) {
            return new BirthDateBuildResult(false,null,BirthDateBuildStatus.InvalidValue);
        }
    }

    public static bool TryApplyOptionalFromParts(
        string? day,
        string? month,
        string? year,
        Action<DateOnly?> applyBirthDate,
        Action<string>? applyError = null) {
        var result = BuildFromParts(day,month,year,isRequired: false);
        if (result.Success) {
            applyBirthDate(result.BirthDate);
            return true;
        }

        applyError?.Invoke(GetErrorMessage(result.Status));
        return false;
    }

    public static string GetErrorMessage(BirthDateBuildStatus status) {
        return status switch {
            BirthDateBuildStatus.Incomplete => "Palun vali sünniajast päev, kuu ja aasta.",
            BirthDateBuildStatus.InvalidValue => "Sünniaja väärtused on vigased.",
            BirthDateBuildStatus.OutOfRange => "Sünniaasta ei ole lubatud vahemikus.",
            BirthDateBuildStatus.FutureDate => "Sünnikuupäev ei saa olla tulevikus.",
            _ => "Valitud sünniaeg ei ole korrektne kuupäev."
        };
    }
}
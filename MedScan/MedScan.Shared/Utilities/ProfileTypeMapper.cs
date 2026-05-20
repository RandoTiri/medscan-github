using MedScan.Shared.Models.Enums;

namespace MedScan.Shared.Utilities;

public static class ProfileTypeMapper {
    public static ProfileTypeEnum? Parse(string? value) {
        return Enum.TryParse<ProfileTypeEnum>(value,ignoreCase: true,out var parsed)
            ? parsed
            : null;
    }

    public static bool IsMainUser(string? value) {
        return Parse(value) == ProfileTypeEnum.Ise;
    }

    public static bool IsPatient(string? value) {
        return Parse(value) == ProfileTypeEnum.Patsient;
    }
}
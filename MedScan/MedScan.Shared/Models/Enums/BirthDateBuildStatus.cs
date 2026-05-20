namespace MedScan.Shared.Models.Enums;

public enum BirthDateBuildStatus {
    Valid,
    Empty,
    Incomplete,
    InvalidValue,
    OutOfRange,
    FutureDate
}
namespace MedScan.Shared.Validation;

public enum BirthDateBuildStatus {
    Valid,
    Empty,
    Incomplete,
    InvalidValue,
    OutOfRange,
    FutureDate
}

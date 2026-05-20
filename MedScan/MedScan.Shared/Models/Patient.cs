using MedScan.Shared.Utilities;

namespace MedScan.Shared.Models;

public sealed class Patient : Profile {
    public string Initials {
        get {
            if (string.IsNullOrWhiteSpace(Name)) {
                return "?";
            }

            var words = Name.Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) {
                return words[0][..1].ToUpperInvariant();
            }

            return (words[0][..1] + words[1][..1]).ToUpperInvariant();
        }
    }

    public int Age {
        get {
            if (!BirthDate.HasValue) {
                return 0;
            }

            return AppDate.CalculateAge(BirthDate);
        }
    }
}
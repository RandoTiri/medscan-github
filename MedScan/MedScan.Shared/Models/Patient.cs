namespace MedScan.Shared.Models;

public sealed class Patient : Profile
{
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "?";
            }

            var words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
            {
                return words[0].Substring(0, 1).ToUpperInvariant();
            }

            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }
    }

    public int Age
    {
        get
        {
            if (!BirthDate.HasValue)
            {
                return 0;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - BirthDate.Value.Year;

            if (today < BirthDate.Value.AddYears(age))
            {
                age--;
            }

            return Math.Max(age, 0);
        }
    }
}

namespace MedScan.Api.Services.Medications;

public static class MedicationConstants {
    public const int LowStockWarningThreshold = 5;
    public const int MinFrequencyPerDay = 1;
    public const int MaxFrequencyPerDay = 24;
    public const int MinWeeklyDay = 0;
    public const int MaxWeeklyDay = 6;

    public const string DefaultScheduledTimeJson = "[\"08:00:00\"]";
    public const string EmptyJsonArray = "[]";

    public static class Messages {
        public const string MedicationNotFoundById = "Medication with id {0} was not found.";
        public const string CreatedReloadFailed = "Created medication could not be reloaded.";
        public const string UpdatedReloadFailed = "Updated medication could not be reloaded.";

        public const string ProfileMissing = "Profiil puudub.";
        public const string MedicationNotFound = "Ravimit ei leitud.";
        public const string StockEmpty = "Ravimit ei ole koduses varus.";
        public const string LastUnitConfirmationNeeded =
            "See on viimane ühik. Võtmisega jätkates pead uue paki ostma.";
        public const string TakeOnceSaved = "Salvestatud logisse, meeldetuletust ei looda.";
        public const string TakeOnceFallbackNote = "Ühekordne võtmine";

        public const string LastPackWarning =
            "PAKI VIIMANE RAVIM. Pärast märkimist kustub see raviskeemist ja ravimite nimekirjast. Kui jätkad  võtmist, skänni uus karp ja lisa ravim uuesti enda raviskeemi.";

        public const string InvalidProfileId = "ProfileId on vigane.";
        public const string InvalidMedicationId = "MedicationId on vigane.";
        public const string InvalidFrequency = "Manustamissagedus peab olema vahemikus 1-24.";
        public const string ScheduledTimeRequired = "Vähemalt üks kellaaeg on kohustuslik.";
        public const string ScheduledTimeCountMismatch =
            "Kellaaegade arv peab vastama manustamissagedusele.";
        public const string WeeklyDaySelectionRequired =
            "Vali iga nädalase manustamise jaoks päev.";
        public const string WeeklyDaysInvalid = "Nädalapäevad on vigased.";
        public const string WeeklyDaysMustBeDistinct = "Nädalapäevad peavad olema erinevad.";

        public static string LowStockRemaining(int remaining) =>
            $"NB seda ravimit on alles vaid {remaining} tk. Kui jätkad sama raviskeemi, osta uus karp.";

        public static string StockBelowRequested(int available) =>
            $"Kodus on alles {available} tk. Vähenda võetavat kogust.";
    }
}
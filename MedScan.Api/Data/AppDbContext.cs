using MedScan.Api.Models;
using MedScan.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser> {
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<UserMedication> UserMedications => Set<UserMedication>();
    public DbSet<DoseLog> DoseLogs => Set<DoseLog>();
    public DbSet<HomePharmacyItem> HomePharmacyItems => Set<HomePharmacyItem>();

    public async Task SeedMedicationsAsync()
    {
        var now = DateTime.UtcNow;

        var medications = new List<Medication>
        {
            new()
            {
                Barcode = "3800010640916",
                Name = "Paracetamol Sopharma",
                ActiveIngredient = "Paratsetamool",
                StrengthMg = "500 MG",
                Indication = "Nõrga kuni mõõduka valu ja palaviku sümptomaatiline ravi.",
                Warnings = "Enne Paracetamol Sopharma võtmist pidage nõu oma arsti või apteekriga, kui teil on mingeid neeru- või maksaprobleeme. Mõnede neeru- või maksahaiguste korral võib teie arstil osutuda vajalikuks teid ravi ajal paratsetamooliga jälgida, et kohandada ravimi annust teie seisundiga. Ärge võtke Paracetamol Sopharmat, kui te olete allergiline (ülitundlik) paratsetamooli või ravimi mis tahes koostisosade suhtes või kui teil on raske maksahaigus.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1552104.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Tabletid",
                Manufacturer = "Sopharma",
                MarketingAuthNr = string.Empty,
                PackSize = "N20",
                CachedAt = now
            },
            new()
            {
                Barcode = "3800010646529",
                Name = "Paracetamol Sopharma",
                ActiveIngredient = "Paratsetamool",
                StrengthMg = "500 MG",
                Indication = "Nõrga kuni mõõduka valu ja palaviku sümptomaatiline ravi.",
                Warnings = "Enne Paracetamol Sopharma võtmist pidage nõu oma arsti või apteekriga, kui teil on mingeid neeru- või maksaprobleeme. Mõnede neeru- või maksahaiguste korral võib teie arstil osutuda vajalikuks teid ravi ajal paratsetamooliga jälgida, et kohandada ravimi annust teie seisundiga. Ärge võtke Paracetamol Sopharmat, kui te olete allergiline (ülitundlik) paratsetamooli või ravimi mis tahes koostisosade suhtes või kui teil on raske maksahaigus.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1685471.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Tabletid",
                Manufacturer = "Sopharma",
                MarketingAuthNr = string.Empty,
                PackSize = "N100",
                CachedAt = now
            },
            new()
            {
                Barcode = "5055565732748",
                Name = "Ibuprofen Accord",
                ActiveIngredient = "Ibuprofenum",
                StrengthMg = "400 MG",
                Indication = "Täiskasvanud ja üle 12-aastased lapsed (>40 kg): palaviku ja nõrga kuni mõõduka valu, sh düsmenorröa, lühiajaline ravi. Krooniliste põletikuliste reumaatiliste haiguste korral valu ja põletiku pikaajaline sümptomaatiline ravi.",
                Warnings = "Põletikuvastaseid/valuvaigistavaid ravimeid, nagu ibuprofeen, võib seostada südameinfarkti- või insuldiriski vähese suurenemisega, eriti suurtes annustes. Ärge ületage soovitatud annust ega ravi kestust. Ravimit ei tohi võtta allergia, raseduse viimase trimestri, raske maksa-/neeru-/südamehaiguse, aktiivse verejooksu või haavandtõve korral.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1673546.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Tabletid",
                Manufacturer = "Accord",
                MarketingAuthNr = string.Empty,
                PackSize = "N30",
                CachedAt = now
            },
            new()
            {
                Barcode = "5010123729189",
                Name = "Sudafed HA Xylo",
                ActiveIngredient = "Ksülometasoliinvesinikkloriid",
                StrengthMg = "1 MG/ML",
                Indication = "Nina limaskesta turse vähendamine nohu korral.",
                Warnings = "Enne kasutamist rääkige arsti või apteekriga, kui teil on südamehaigused (nt pikenenud QT-sündroom). Pikaajaline kasutamine võib põhjustada kroonilist nina limaskesta turset. Ei tohi kasutada, kui olete ksülometasoliini suhtes allergiline või alla 6-aastastel lastel.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1205985.pdf",
                MethodOfAdministraion = "Nina kaudu",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Ninasprei",
                Manufacturer = "Famar Health Care Services",
                MarketingAuthNr = string.Empty,
                PackSize = "10ML",
                CachedAt = now
            },
            new()
            {
                Barcode = "4013054029832",
                Name = "Espumisan Forte",
                ActiveIngredient = "Simetikoon",
                StrengthMg = "240 MG",
                Indication = "Soolegaasidest põhjustatud seedetrakti vaevuste sümptomaatiline ravi. Täiendava vahendina kõhupiirkonna diagnostilistel uuringutel.",
                Warnings = "Kui kõhukaebused püsivad või korduvad 3 päeva jooksul, pöörduge arsti poole. Ravimit ei tohi võtta, kui olete simetikooni või ravimi koostisosade suhtes allergiline.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_3030332.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Kapslid",
                Manufacturer = "Berlin-Chemie AG",
                MarketingAuthNr = string.Empty,
                PackSize = "N20",
                CachedAt = now
            },
            new()
            {
                Barcode = "04030855234233",
                Name = "Flux",
                ActiveIngredient = "Fluoxetinum",
                StrengthMg = "20 MG",
                Indication = "Antidepressant (SSRI), kasutatakse depressiooni, obsessiiv-kompulsiivse häire ja buliimia raviks.",
                Warnings = "Enne kasutamist pidage nõu arstiga, eriti kui teil on maksa- või neerutalitluse häire, epilepsia, südamehaigus, silmahaigus või veritsushäired. Ei tohi võtta allergia, metoprolooli kasutamise ega MAO inhibiitorite samaaegse/raskelt ajastatud kasutamise korral.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1107122.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Retseptiravim",
                MedicationForm = "Kapslid",
                Manufacturer = "Sandoz",
                MarketingAuthNr = string.Empty,
                PackSize = "N28",
                CachedAt = now
            },
            new()
            {
                Barcode = "05290931027022",
                Name = "Fluconazole Medochemie",
                ActiveIngredient = "Fluconazolum",
                StrengthMg = "150 MG",
                Indication = "Suguelundite seenpõletiku raviks naistel (tupe) või meestel (eesnaha seeninfektsioon).",
                Warnings = "Ei tohi võtta, kui olete flukonasooli suhtes allergiline või kasutate astemisooli, terfenadiini, tsisapriidi, pimosiidi, kinidiini või erütromütsiini.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1140703.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Kapslid",
                Manufacturer = "Medochemie",
                MarketingAuthNr = string.Empty,
                PackSize = "N1",
                CachedAt = now
            },
            new()
            {
                Barcode = "07613421029043",
                Name = "Omep Uno",
                ActiveIngredient = "Omeprazolum",
                StrengthMg = "20 MG",
                Indication = "Gastroösofageaalse reflukshaiguse vaevuste (kõrvetised ja mao-söögitoru tagasivoolusümptomid) ravi täiskasvanutel.",
                Warnings = "Ärge võtke üle 14 päeva ilma arstiga nõu pidamata. Ei tohi võtta allergia korral prootonpumba inhibiitorite suhtes või nelfinaviiri kasutamisel.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1479124.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Gastroresistentsed kõvakapslid",
                Manufacturer = "Sandoz",
                MarketingAuthNr = string.Empty,
                PackSize = "N14",
                CachedAt = now
            },
            new()
            {
                Barcode = "03582910055372",
                Name = "Cardace",
                ActiveIngredient = "Ramiprilum",
                StrengthMg = "5 MG",
                Indication = "Cardace’t võib kasutada kõrgenenud vererõhu raviks, südameataki või insuldi riski vähendamiseks, neerukahjustuse süvenemise riski vähendamiseks ning südamepuudulikkuse raviks, sh pärast müokardiinfarkti.",
                Warnings = "Cardace’t ei tohi võtta, kui olete ramipriili või teiste AKE inhibiitorite suhtes allergiline, kui teil on olnud angioödeem, kui kasutate sakubitriili/valsartaani, kui saate dialüüsi, kui teil on neeruarteri stenoos, raseduse viimasel 6 kuul, ebanormaalselt madala vererõhu korral või kui teil on diabeet/neerukahjustus ja kasutate aliskireeni.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1502198.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Retseptiravim",
                MedicationForm = "Tabletid",
                Manufacturer = "Sanofi",
                MarketingAuthNr = string.Empty,
                PackSize = "N56",
                CachedAt = now
            },
            new()
            {
                Barcode = "4742041002907",
                Name = "Coldrex MaxGrip Menthol & Berries",
                ActiveIngredient = "Paracetamolum + Phenylephrinum + Acidum ascorbicum",
                StrengthMg = "1000 MG + 10 MG + 70 MG",
                Indication = "Lühiajaline palaviku alandamine, nõrga valu leevendamine ja nohu sümptomaatiline ravi täiskasvanutel ja üle 16-aastastel lastel.",
                Warnings = "Ärge võtke koos teiste külmetus- ja gripiravimitega. Liiga suur paratsetamooli kogus võib põhjustada tõsist maksakahjustust või maksapuudulikkust. Ärge kasutage samaaegselt teiste paratsetamooli sisaldavate ravimitega.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1500747.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Suukaudse lahuse pulber",
                Manufacturer = "Omega Pharma International NV",
                MarketingAuthNr = string.Empty,
                PackSize = "N10",
                CachedAt = now
            },
            new()
            {
                Barcode = "5000158104273",
                Name = "Nurofen Forte Express",
                ActiveIngredient = "Ibuprofenum",
                StrengthMg = "400 MG",
                Indication = "Nõrk või mõõdukas valu, düsmenorröa ja palavik.",
                Warnings = "Ei tohi kasutada, kui olete ibuprofeeni või teiste MSPVA-de suhtes allergiline, raseduse viimasel trimestril, maohaavandi/perforatsiooni/seedetrakti verejooksu korral, raskete maksa-, neeru- või südameprobleemide korral ning alla 12-aastastel.",
                PdfUrl = "https://www.ravimiregister.ee/Data/PIL/PIL_1285646.pdf",
                MethodOfAdministraion = "Suukaudne",
                PrescriptionType = "Käsimüügiravim",
                MedicationForm = "Tabletid",
                Manufacturer = "RB NL Brands B.V.",
                MarketingAuthNr = string.Empty,
                PackSize = "N12",
                CachedAt = now
            }
        };

        var seededBarcodes = medications
            .Select(x => x.Barcode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var existing = await Medications.ToListAsync();
        var existingByBarcode = existing
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .ToDictionary(x => x.Barcode, x => x, StringComparer.Ordinal);

        foreach (var seed in medications)
        {
            if (existingByBarcode.TryGetValue(seed.Barcode, out var current))
            {
                current.Name = seed.Name;
                current.ActiveIngredient = seed.ActiveIngredient;
                current.StrengthMg = seed.StrengthMg;
                current.PackSize = seed.PackSize;
                current.Indication = seed.Indication;
                current.Warnings = seed.Warnings;
                current.PdfUrl = seed.PdfUrl;
                current.MethodOfAdministraion = seed.MethodOfAdministraion;
                current.PrescriptionType = seed.PrescriptionType;
                current.MedicationForm = seed.MedicationForm;
                current.Manufacturer = seed.Manufacturer;
                current.MarketingAuthNr = seed.MarketingAuthNr;
                current.CachedAt = now;
            }
            else
            {
                seed.CachedAt = now;
                await Medications.AddAsync(seed);
            }
        }

        var extraRows = existing
            .Where(x => string.IsNullOrWhiteSpace(x.Barcode) || !seededBarcodes.Contains(x.Barcode))
            .ToList();

        if (extraRows.Count > 0)
        {
            Medications.RemoveRange(extraRows);
        }

        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<Profile>(entity =>
        {
            entity.Property(x => x.ProfileType).HasColumnName("Type");
            entity.HasIndex(x => x.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserMedication>(entity =>
        {
            entity.Property(x => x.Frequency).HasColumnName("FrequencyPerDay");
            entity.Property(x => x.ScheduleUnit).HasConversion<int>();
            entity.Property(x => x.WeeklyDaysJson).HasDefaultValue("[]");
            entity.Property(x => x.StartDate).HasColumnType("date");
        });

        builder.Entity<Medication>(entity =>
        {
            entity.Property(x => x.MarketingAuthNr).HasColumnName("MarketingAuthNumber");
            entity.Property(x => x.MethodOfAdministraion).HasColumnName("MethodOfAdministrion");
        });

        builder.Entity<DoseLog>(entity =>
        {
            entity.Property(x => x.DoseStatus).HasColumnName("Status");
        });

        builder.Entity<HomePharmacyItem>(entity =>
        {
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_HomePharmacyItems_Quantity_Positive", "\"Quantity\" > 0");
            });
            entity.HasIndex(x => x.ProfileId);
            entity.HasIndex(x => x.MedicationId);
            entity.HasIndex(x => x.ExpiresOn);
            entity.Property(x => x.Quantity).HasDefaultValue(1);
            entity.Property(x => x.BatchNumber).HasMaxLength(100);
            entity.Property(x => x.ExpiresOn).HasColumnType("date");
            entity.Property(x => x.AddedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.Profile)
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Medication)
                .WithMany()
                .HasForeignKey(x => x.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


using MedScan.Api.Data;
using MedScan.Api.Models;
using MedScan.Api.Repositories;
using MedScan.Api.Services;
using MedScan.Shared.Models;
using MedScan.Shared.Models.Enums;
using MedScan.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IUserMedicationRepository, UserMedicationRepository>();
builder.Services.AddScoped<IHomePharmacyRepository, HomePharmacyRepository>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IMedicationCatalogService, MedicationCatalogService>();
builder.Services.AddScoped<MedScan.Api.Services.IHomePharmacyService, HomePharmacyService>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddApiEndpoints();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MedScan.Api",
        Version = "v1"
    });

    const string schemeId = "Bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Sisesta JWT token."
    });

    options.AddSecurityRequirement(document =>
    {
        return new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = []
        };
    });

    options.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Pattern = "^([01]\\d|2[0-3]):[0-5]\\d:[0-5]\\d$"
    });

    options.MapType<TimeOnly?>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Pattern = "^([01]\\d|2[0-3]):[0-5]\\d:[0-5]\\d$"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await SeedMedicationsAsync(app.Services);

app.Run();

static async Task SeedMedicationsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (await dbContext.Medications.AnyAsync())
    {
        return;
    }

    var now = DateTime.UtcNow;

    var medications = new List<Medication>
    {
        new()
        {
            Barcode = "4740006010012",
            Name = "Paratsetamool",
            ActiveIngredient = "Paratsetamool",
            StrengthMg = "500 mg",
            PackSize = "N20",
            Indication = "Valu ja palavik",
            Warnings = "Mitte ületada ööpäevast annust.",
            MedicationForm = MedicationFormEnum.Tablett,
            MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
            PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
            Manufacturer = "Test Pharma",
            MarketingAuthNr = "TEST-001",
            CachedAt = now
        },
        new()
        {
            Barcode = "4740006010029",
            Name = "Ibuprofeen",
            ActiveIngredient = "Ibuprofeen",
            StrengthMg = "400 mg",
            PackSize = "N20",
            Indication = "Põletik ja valu",
            Warnings = "Võtta koos toiduga.",
            MedicationForm = MedicationFormEnum.Tablett,
            MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
            PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
            Manufacturer = "Test Pharma",
            MarketingAuthNr = "TEST-002",
            CachedAt = now
        },
        new()
        {
            Barcode = "4740006010036",
            Name = "Amoksitsilliin",
            ActiveIngredient = "Amoksitsilliin",
            StrengthMg = "500 mg",
            PackSize = "N20",
            Indication = "Bakteriaalsed infektsioonid",
            Warnings = "Kasutada arsti juhisel.",
            MedicationForm = MedicationFormEnum.Kapsel,
            MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
            PrescriptionType = PrescriptionTypeEnum.Retseptiravim,
            Manufacturer = "Test Pharma",
            MarketingAuthNr = "TEST-003",
            CachedAt = now
        },
        new()
        {
            Barcode = "4740006010043",
            Name = "Metformiin",
            ActiveIngredient = "Metformiin",
            StrengthMg = "500 mg",
            PackSize = "N30",
            Indication = "2. tüüpi diabeet",
            Warnings = "Võtta vastavalt raviskeemile.",
            MedicationForm = MedicationFormEnum.Tablett,
            MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
            PrescriptionType = PrescriptionTypeEnum.Retseptiravim,
            Manufacturer = "Test Pharma",
            MarketingAuthNr = "TEST-004",
            CachedAt = now
        },
        new()
        {
            Barcode = "4740006010050",
            Name = "Loratadiin",
            ActiveIngredient = "Loratadiin",
            StrengthMg = "10 mg",
            PackSize = "N10",
            Indication = "Allergia sümptomid",
            Warnings = "Võib põhjustada uimasust.",
            MedicationForm = MedicationFormEnum.Tablett,
            MethodOfAdministraion = MethodOfAdministraionEnum.Suukaudne,
            PrescriptionType = PrescriptionTypeEnum.Kasimuugiravim,
            Manufacturer = "Test Pharma",
            MarketingAuthNr = "TEST-005",
            CachedAt = now
        }
    };

    await dbContext.Medications.AddRangeAsync(medications);
    await dbContext.SaveChangesAsync();
}

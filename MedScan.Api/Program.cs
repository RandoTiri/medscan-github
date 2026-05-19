using MedScan.Api.Data;
using MedScan.Api.Data.Identity;
using MedScan.Api.Options;
using MedScan.Api.Repositories;
using MedScan.Api.Repositories.Medications;
using MedScan.Api.Services;
using MedScan.Api.Services.Auth;
using MedScan.Api.Services.Catalog;
using MedScan.Api.Services.Medications;
using MedScan.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IUserMedicationRepository, UserMedicationRepository>();
builder.Services.AddScoped<IHomePharmacyRepository, HomePharmacyRepository>();
builder.Services.AddScoped<IDoseLogRepository,DoseLogRepository>();
builder.Services.AddScoped<IProfileRepository,ProfileRepository>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<DoseLogService>();
builder.Services.AddScoped<MedicationStockService>();
builder.Services.AddScoped<TakeMedicationOnceService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IMedicationCatalogService, MedicationCatalogService>();
builder.Services.AddScoped<IHomePharmacyService, HomePharmacyService>();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");
    logger.LogInformation("Applying database migrations...");
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully.");

    logger.LogInformation("Seeding medications...");
    await dbContext.SeedMedicationsAsync();
    logger.LogInformation("Medication seeding completed.");
}

app.Run();

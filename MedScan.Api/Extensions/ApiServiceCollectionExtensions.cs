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

namespace MedScan.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    private const string CorsPolicyName = "AllowFrontend";

    public static IServiceCollection AddApiPersistence(this IServiceCollection services,IConfiguration configuration) {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddApiDomainServices(this IServiceCollection services) {
        services.AddScoped<IMedicationRepository,MedicationRepository>();
        services.AddScoped<IUserMedicationRepository,UserMedicationRepository>();
        services.AddScoped<IHomePharmacyRepository,HomePharmacyRepository>();
        services.AddScoped<IDoseLogRepository,DoseLogRepository>();
        services.AddScoped<IProfileRepository,ProfileRepository>();
        services.AddScoped<IPasswordResetService,PasswordResetService>();
        services.AddScoped<DoseLogService>();
        services.AddScoped<MedicationStockService>();
        services.AddScoped<TakeMedicationOnceService>();
        services.AddScoped<IMedicationService,MedicationService>();
        services.AddScoped<IMedicationCatalogService,MedicationCatalogService>();
        services.AddScoped<IHomePharmacyService,HomePharmacyService>();

        return services;
    }

    public static IServiceCollection AddApiIdentity(this IServiceCollection services) {
        services
            .AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);

        services
            .AddIdentityCore<ApplicationUser>(options => {
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddApiEndpoints();

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services) {
        services.AddCors(options => {
            options.AddPolicy(CorsPolicyName,policy => {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static IServiceCollection AddApiSwagger(this IServiceCollection services) {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1",new OpenApiInfo {
                Title = "MedScan.Api",
                Version = "v1"
            });

            const string schemeId = "Bearer";

            options.AddSecurityDefinition(schemeId,new OpenApiSecurityScheme {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Sisesta JWT token."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement {
                    [new OpenApiSecuritySchemeReference(schemeId,document)] = []
                });

            options.MapType<TimeOnly>(() => CreateTimeOnlySchema());
            options.MapType<TimeOnly?>(() => CreateTimeOnlySchema());
        });

        return services;
    }

    public static IApplicationBuilder UseApiCors(this IApplicationBuilder app) {
        app.UseCors(CorsPolicyName);
        return app;
    }

    private static OpenApiSchema CreateTimeOnlySchema() =>
        new() {
            Type = JsonSchemaType.String,
            Pattern = "^([01]\\d|2[0-3]):[0-5]\\d:[0-5]\\d$"
        };
}
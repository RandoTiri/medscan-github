using MedScan.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Extensions;

public static class ApiDatabaseStartupExtensions {
    public static async Task ApplyDatabaseStartupAsync(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding medications...");
        await dbContext.SeedMedicationsAsync();
        logger.LogInformation("Medication seeding completed.");
    }
}

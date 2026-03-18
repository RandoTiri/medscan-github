using MedScan.Api.Contracts;
using MedScan.Api.Data;
using MedScan.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services
    .AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddAuthorization();

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

app.MapIdentityApi<ApplicationUser>();

app.MapPost("/api/auth/register", async (
    [FromBody] AppRegisterRequest request,
    [FromServices] UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Kõik väljad on kohustuslikud." });
    }

    var existingUser = await userManager.FindByEmailAsync(request.Email);
    if (existingUser is not null)
    {
        return Results.BadRequest(new { message = "Selle emailiga kasutaja on juba olemas." });
    }

    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FullName = request.FullName,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.BadRequest(result.Errors.Select(e => new
        {
            e.Code,
            e.Description
        }));
    }

    return Results.Ok(new
    {
        message = "User created"
    });
});

app.MapGet("/api/auth/me", async (
    HttpContext httpContext,
    [FromServices] UserManager<ApplicationUser> userManager) =>
{
    if (httpContext.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var user = await userManager.GetUserAsync(httpContext.User);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        user.Id,
        user.FullName,
        user.Email
    });
}).RequireAuthorization();


app.Run();
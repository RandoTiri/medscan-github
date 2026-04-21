using MedScan.Shared.Services;
using MedScan.Web.Components;
using MedScan.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the MedScan.Shared project
builder.Services.AddSingleton<IFormFactor,FormFactor>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IExternalNavigationService, WebExternalNavigationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error",createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found",createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(MedScan.Shared._Imports).Assembly);

app.Run();

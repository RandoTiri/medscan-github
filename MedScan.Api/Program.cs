using MedScan.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiPersistence(builder.Configuration);
builder.Services.AddApiDomainServices();
builder.Services.AddApiIdentity();
builder.Services.AddApiCors();
builder.Services.AddApiSwagger();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseApiCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.ApplyDatabaseStartupAsync();

app.Run();
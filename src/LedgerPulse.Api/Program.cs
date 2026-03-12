using LedgerPulse.Api.Endpoints;
using LedgerPulse.Application;
using LedgerPulse.Infrastructure;
using LedgerPulse.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapLedgerEndpoints();
app.MapDailyConsolidationEndpoints();

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;

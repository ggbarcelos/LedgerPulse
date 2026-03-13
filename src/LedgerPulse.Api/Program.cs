using System.Threading.RateLimiting;
using LedgerPulse.Api.Endpoints;
using LedgerPulse.Api.Security;
using LedgerPulse.Application;
using LedgerPulse.Infrastructure;
using LedgerPulse.Infrastructure.Extensions;
using Microsoft.AspNetCore.RateLimiting;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ApiKeyEndpointFilter>();
builder.Services.AddRateLimiter(options =>
{
    var ledgerWritePermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:LedgerWrite:PermitLimit") ?? 55;
    var ledgerWriteWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:LedgerWrite:WindowSeconds") ?? 1;
    var ledgerWriteQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:LedgerWrite:QueueLimit") ?? 5;

    var processPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:ConsolidationProcess:PermitLimit") ?? 5;
    var processWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:ConsolidationProcess:WindowSeconds") ?? 1;
    var processQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:ConsolidationProcess:QueueLimit") ?? 2;

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("LedgerWrite", limiterOptions =>
    {
        limiterOptions.PermitLimit = Math.Max(1, ledgerWritePermitLimit);
        limiterOptions.Window = TimeSpan.FromSeconds(Math.Max(1, ledgerWriteWindowSeconds));
        limiterOptions.QueueLimit = Math.Max(0, ledgerWriteQueueLimit);
        limiterOptions.AutoReplenishment = true;
    });

    options.AddFixedWindowLimiter("ConsolidationProcess", limiterOptions =>
    {
        limiterOptions.PermitLimit = Math.Max(1, processPermitLimit);
        limiterOptions.Window = TimeSpan.FromSeconds(Math.Max(1, processWindowSeconds));
        limiterOptions.QueueLimit = Math.Max(0, processQueueLimit);
        limiterOptions.AutoReplenishment = true;
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapLedgerEndpoints();
app.MapDailyConsolidationEndpoints();

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;

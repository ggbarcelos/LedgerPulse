using System.Security.Cryptography;
using System.Text;

namespace LedgerPulse.Api.Security;

public sealed class ApiKeyEndpointFilter(IConfiguration configuration, ILogger<ApiKeyEndpointFilter> logger) : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requirement = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ApiKeyRequirementMetadata>();
        if (requirement is null)
        {
            return await next(context);
        }

        var expectedApiKey = configuration[requirement.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            logger.LogWarning(
                "API key validation was skipped for {Method} {Path}. Requirement {RequirementName} is not configured at {ConfigurationKey}.",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                requirement.RequirementName,
                requirement.ConfigurationKey);
            return await next(context);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedApiKey))
        {
            logger.LogWarning(
                "Unauthorized request to {Method} {Path}. Missing {HeaderName} for requirement {RequirementName}. RemoteIp={RemoteIp}.",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                HeaderName,
                requirement.RequirementName,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Results.Unauthorized();
        }

        if (!IsMatch(providedApiKey.ToString(), expectedApiKey))
        {
            logger.LogWarning(
                "Unauthorized request to {Method} {Path}. Invalid API key for requirement {RequirementName}. RemoteIp={RemoteIp}.",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                requirement.RequirementName,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static bool IsMatch(string providedApiKey, string expectedApiKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}

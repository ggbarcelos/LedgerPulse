namespace LedgerPulse.Api.Security;

public sealed class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly string? apiKey = configuration["ApiSecurity:ApiKey"];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return await next(context);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedApiKey))
        {
            return Results.Unauthorized();
        }

        if (!string.Equals(providedApiKey.ToString(), apiKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}


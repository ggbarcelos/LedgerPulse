namespace LedgerPulse.Api.Security;

public static class ApiKeyEndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequireApiKey(
        this RouteHandlerBuilder builder,
        string configurationKey,
        string requirementName)
    {
        return builder
            .WithMetadata(new ApiKeyRequirementMetadata(configurationKey, requirementName))
            .AddEndpointFilter<ApiKeyEndpointFilter>();
    }
}


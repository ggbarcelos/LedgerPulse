using System.Globalization;
using LedgerPulse.Frontend;
using LedgerPulse.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var ptBrCulture = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBrCulture;
CultureInfo.DefaultThreadCurrentUICulture = ptBrCulture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
    var baseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
        ? new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute)
        : new Uri(new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute), apiBaseUrl);

    var httpClient = new HttpClient { BaseAddress = baseAddress };

    return new LedgerPulseApiClient(httpClient, builder.Configuration["ApiKey"]);
});

await builder.Build().RunAsync();

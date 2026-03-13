using LedgerPulse.Frontend;
using LedgerPulse.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

    return new LedgerPulseApiClient(httpClient);
});

await builder.Build().RunAsync();

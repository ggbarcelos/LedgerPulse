using LedgerPulse.Frontend;
using LedgerPulse.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080/";
    return new LedgerPulseApiClient(new HttpClient { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) });
});

await builder.Build().RunAsync();

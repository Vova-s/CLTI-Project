using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using LoginPage.Client;
using LoginPage.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register root components for the app and the head outlet.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Retrieve ApiBaseUrl from configuration.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

// Validate that the ApiBaseUrl is configured.
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}

// Configure HttpClient with the base address from configuration.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Add authorization and authentication services.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<StateService>();

// Register Blazored LocalStorage for managing local storage operations.
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();

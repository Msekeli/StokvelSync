using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StokvelSync.Client;
using StokvelSync.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Root components for the UI
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Base HttpClient: Required for Blazor to load its own internal files
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// 2. User Session: This is the shared memory for your Email state
builder.Services.AddScoped<UserSession>();

// 3. Azure API Client: Named specifically to avoid overwriting the base client
// Note: localhost:7071 is the default port for Azure Functions Core Tools
builder.Services.AddHttpClient("AzureApi", client => 
{
    client.BaseAddress = new Uri("http://localhost:7071/api/");
});

await builder.Build().RunAsync();
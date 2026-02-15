using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;
using Azure.Storage.Blobs; // Required for Receipt Uploads
using StokvelSync.Api.Services;
using StokvelSync.Api.Data;

var host = new HostBuilder()
    // Using ConfigureFunctionsWebApplication for optimal local and cloud performance
    .ConfigureFunctionsWebApplication() 
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Registering your business logic services
        services.AddSingleton<PenaltyService>();
        services.AddSingleton<MemberRepository>();

        // Fetch connection string once for both services
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

        // Database Connection (Table Storage for Member Data & Penalties)
        services.AddSingleton(sp => 
        {
            return new TableServiceClient(connectionString);
        });

        // NEW: Storage Connection (Blob Storage for Receipt Images)
        // This fixes the 'Azure.Storage' namespace error in ReceiptFunctions.cs
        services.AddSingleton(sp => 
        {
            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
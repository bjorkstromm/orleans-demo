using System.Net;
using Azure.Identity;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseOrleans((context, siloBuilder) =>
    {
        var storageName = context.Configuration.GetValue<string>("AZURE_STORAGE_NAME");
        var managedIdentityClientId = context.Configuration.GetValue<string>("MANAGEDIDENTITY_CLIENTID");
        var connectionString = context.Configuration.GetValue<string>("AzureWebJobsStorage");
        var useManagedIdentity = !string.IsNullOrWhiteSpace(storageName) && !string.IsNullOrWhiteSpace(managedIdentityClientId);

        if (context.HostingEnvironment.IsDevelopment())
        {
            siloBuilder.ConfigureEndpoints(IPAddress.Loopback, siloPort: 11_111, gatewayPort: 30_000);
        }

        siloBuilder.Services.AddApplicationInsightsTelemetryWorkerService();

        siloBuilder.Configure<SiloOptions>(options =>
        {
            options.SiloName = "Booking.Silo";
        });
        siloBuilder.UseAzureStorageClustering(
            options =>
            {
                if (useManagedIdentity)
                {
                    var uri = new Uri($"https://{storageName}.table.core.windows.net/");
                    options.ConfigureTableServiceClient(uri, new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = managedIdentityClientId
                    }));
                }
                else
                {
                    options.ConfigureTableServiceClient(connectionString);
                }
            });
        siloBuilder.AddAzureBlobGrainStorageAsDefault(
            options =>
            {
                if (useManagedIdentity)
                {
                    var uri = new Uri($"https://{storageName}.blob.core.windows.net/");
                    options.ConfigureBlobServiceClient(uri, new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = managedIdentityClientId
                    }));
                }
                else
                {
                    options.ConfigureBlobServiceClient(connectionString);
                }
            });
        siloBuilder.UseAzureTableReminderService(
            options =>
            {
                if (useManagedIdentity)
                {
                    var uri = new Uri($"https://{storageName}.table.core.windows.net/");
                    options.ConfigureTableServiceClient(uri, new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = managedIdentityClientId
                    }));
                }
                else
                {
                    options.ConfigureTableServiceClient(connectionString);
                }
            });
        siloBuilder.UseDashboard();
    });

var app = builder.Build();

app.UseOrleansDashboard();
app.Run();
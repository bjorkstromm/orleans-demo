using System.Net;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;

await Host.CreateDefaultBuilder(args)
    .UseOrleans((context, builder) =>
    {
        var storageName = context.Configuration.GetValue<string>("AZURE_STORAGE_NAME");
        var managedIdentityClientId = context.Configuration.GetValue<string>("MANAGEDIDENTITY_CLIENTID");
        var connectionString = context.Configuration.GetValue<string>("AzureWebJobsStorage");
        var useManagedIdentity = !string.IsNullOrWhiteSpace(storageName) && !string.IsNullOrWhiteSpace(managedIdentityClientId);

        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.ConfigureEndpoints(IPAddress.Loopback, siloPort: 11_111, gatewayPort: 30_000);
        }

        builder.Services.AddApplicationInsightsTelemetryWorkerService();

        builder.Configure<SiloOptions>(options =>
        {
            options.SiloName = "Booking.Silo";
        });
        builder.UseAzureStorageClustering(
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
        builder.AddAzureBlobGrainStorageAsDefault(
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
        builder.UseAzureTableReminderService(
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
    })
    .RunConsoleAsync();
using System.Net;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

        siloBuilder.Services.AddApplicationInsightsTelemetry();

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

        siloBuilder.AddActivityPropagation();
    });

var applicationInsightsConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddMeter("Microsoft.Orleans");

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = applicationInsightsConnectionString;
            });
        }
    })
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: "booking-silo"));

        tracing.AddAspNetCoreInstrumentation();
        tracing.AddSource("Microsoft.Orleans.Runtime");
        tracing.AddSource("Microsoft.Orleans.Application");
        tracing.AddSource("Booking");

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = applicationInsightsConnectionString;
            });
        }
    }).StartWithHost();

var app = builder.Build();

app.UseOrleansDashboard();
app.Run();
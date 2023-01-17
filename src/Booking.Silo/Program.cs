using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Serialization;

await Host.CreateDefaultBuilder(args)
    .UseOrleans((context, builder) =>
    {
        var connectionString = context.Configuration.GetValue<string>("AzureWebJobsStorage");

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
            options => options.ConfigureTableServiceClient(connectionString));
        builder.AddAzureBlobGrainStorageAsDefault(
            options => options.ConfigureBlobServiceClient(connectionString));
        builder.UseAzureTableReminderService(options =>
            options.ConfigureTableServiceClient(connectionString));

        builder.Services.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddNewtonsoftJsonSerializer(
                isSupported: type => type.Namespace?.StartsWith("Newtonsoft.Json") ?? false);
        });
    })
    .RunConsoleAsync();
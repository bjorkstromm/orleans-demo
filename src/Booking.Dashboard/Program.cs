using System.Net;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleans((context, siloBuilder) =>
{
    var connectionString = context.Configuration.GetValue<string>("AzureWebJobsStorage");

    if (context.HostingEnvironment.IsDevelopment())
    {
        siloBuilder.ConfigureEndpoints(IPAddress.Loopback, siloPort: 11_112, gatewayPort: 30_001);
    }

    siloBuilder.Configure<SiloOptions>(options =>
    {
        options.SiloName = "Booking.Dashboard";
    });
    siloBuilder.UseAzureStorageClustering(
        options => options.ConfigureTableServiceClient(connectionString));
    siloBuilder.UseDashboard();
});

var app = builder.Build();

app.UseOrleansDashboard();
app.Run();
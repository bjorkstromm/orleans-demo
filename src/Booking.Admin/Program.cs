using System.Net;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleansClient(clientBuilder =>
{
    var storageName = builder.Configuration.GetValue<string>("AZURE_STORAGE_NAME");
    var managedIdentityClientId = builder.Configuration.GetValue<string>("MANAGEDIDENTITY_CLIENTID");
    var connectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage");
    var useManagedIdentity = !string.IsNullOrWhiteSpace(storageName) && !string.IsNullOrWhiteSpace(managedIdentityClientId);

    clientBuilder.UseAzureStorageClustering(
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

    clientBuilder.AddActivityPropagation();
});

// Add services to the container.

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

var applicationInsightsConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");

var otResourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(serviceName: "booking-admin");

builder.Logging.AddOpenTelemetry(options => options
    .SetResourceBuilder(otResourceBuilder)
    .AddOtlpExporter());

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(otResourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddMeter("Microsoft.Orleans");

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = applicationInsightsConnectionString;
            });
        }
    })
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddSource("Microsoft.Orleans.Runtime")
        .AddSource("Microsoft.Orleans.Application")
        .AddSource("Booking")
        .AddOtlpExporter());

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.224.0.0"), 16));
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
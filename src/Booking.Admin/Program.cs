using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

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
});

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

// For some reason forwarded headers are not working in Azure Container Aoos
if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next(context);
    });
}

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

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

using Azure.Identity;

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
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

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
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
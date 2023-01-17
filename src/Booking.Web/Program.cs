using Orleans.Configuration;
using Orleans.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleansClient(clientBuilder =>
{
    var connectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage");

    clientBuilder.UseAzureStorageClustering(
        options => options.ConfigureTableServiceClient(connectionString));

    clientBuilder.Services.AddSerializer(serializerBuilder =>
    {
        serializerBuilder.AddNewtonsoftJsonSerializer(
            isSupported: type => type.Namespace?.StartsWith("Newtonsoft.Json") ?? false);
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
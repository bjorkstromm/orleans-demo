using Booking.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    var connectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage");

    siloBuilder.UseAzureStorageClustering(
        options => options.ConfigureTableServiceClient(connectionString));
    siloBuilder.AddAzureBlobGrainStorageAsDefault(
        options => options.ConfigureBlobServiceClient(connectionString));
    siloBuilder.UseAzureTableReminderService(options =>
        options.ConfigureTableServiceClient(connectionString));
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

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
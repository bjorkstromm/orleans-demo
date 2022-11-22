using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reentrancy;

var host = Host.CreateDefaultBuilder()
    .UseOrleans(siloBuilder => siloBuilder.UseLocalhostClustering())
    .Build();

var grainFactory = host.Services.GetRequiredService<IGrainFactory>();

await host.StartAsync();

await (args.FirstOrDefault()?.ToLowerInvariant() switch
{
    "reentrant" => ReentrantSample.Run(grainFactory),
    "interleave" => AlwaysInterleaveSample.Run(grainFactory),
    "readonly" => ReadOnlySample.Run(grainFactory),
    _ => NonReentrantSample.Run(grainFactory),
});

await host.StopAsync();
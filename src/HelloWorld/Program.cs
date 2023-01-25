// Host configuration
var host = Host.CreateDefaultBuilder()
    .UseOrleans(x => x.UseLocalhostClustering())
    .Build();

var client = host.Services.GetRequiredService<IClusterClient>();

await host.StartAsync();

// Grain calls
var grain = client.GetGrain<IGreeterGrain>(Guid.NewGuid());
var response = await grain.SayHello("Martin");
Console.WriteLine(response);

await host.StopAsync();

// Grain interface and implementation
public interface IGreeterGrain : IGrainWithGuidKey
{
    Task<string> SayHello(string name);
}

public class GreeterGrain : Grain, IGreeterGrain
{
    public Task<string> SayHello(string name) =>
        Task.FromResult($"Hello {name} from {IdentityString}!");
}
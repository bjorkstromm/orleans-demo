using Orleans.Concurrency;

namespace Reentrancy;

public static class NonReentrantSample
{
    public static async Task Run(IGrainFactory factory)
    {
        var grain = factory.GetGrain<INonReentrantGrain>(Guid.NewGuid());

        await Task.WhenAll(
            grain.DoWork("1"),
            grain.DoWork("2"),
            grain.DoWork("3"));
    }
}

public interface INonReentrantGrain : IGrainWithGuidKey
{
    Task DoWork(string str);
}

public class NonReentrantGrain : Grain, INonReentrantGrain
{
    public async Task DoWork(string str)
    {
        Console.WriteLine($"{str} - First");
        await Task.Delay(1000);
        Console.WriteLine($"{str} - Second");
        await Task.Delay(1000);
        Console.WriteLine($"{str} - Third");
        await Task.Delay(1000);
    }
}
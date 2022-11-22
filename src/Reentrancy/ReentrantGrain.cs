using Orleans.Concurrency;

namespace Reentrancy;

public static class ReentrantSample
{
    public static async Task Run(IGrainFactory factory)
    {
        var grain = factory.GetGrain<IReentrantGrain>(Guid.NewGuid());

        await Task.WhenAll(
            grain.DoWork("1"),
            grain.DoWork("2"),
            grain.DoWork("3"));
    }
}

public interface IReentrantGrain : IGrainWithGuidKey
{
    Task DoWork(string str);
}

[Reentrant]
public class ReentrantGrain : Grain, IReentrantGrain
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
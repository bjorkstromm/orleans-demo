using Orleans.Concurrency;

namespace Reentrancy;

public static class ReadOnlySample
{
    public static async Task Run(IGrainFactory factory)
    {
        var grain = factory.GetGrain<IReadOnlyGrain>(Guid.NewGuid());

        await Task.WhenAll(
            grain.Fast("1"),
            grain.Fast("2"),
            grain.Fast("3"),
            grain.Slow("4 (slow)"),
            grain.Fast("5"),
            grain.Fast("6"));
    }
}

public interface IReadOnlyGrain : IGrainWithGuidKey
{
    [ReadOnly]
    Task Fast(string str);

    Task Slow(string str);
}

public class ReadOnlyGrain : Grain, IReadOnlyGrain
{
    public Task Fast(string str) => DoWork(str);

    public Task Slow(string str) => DoWork(str);

    private async Task DoWork(string str)
    {
        Console.WriteLine($"{str} - First");
        await Task.Delay(100);
        Console.WriteLine($"{str} - Second");
        await Task.Delay(100);
        Console.WriteLine($"{str} - Third");
        await Task.Delay(100);
    }
}
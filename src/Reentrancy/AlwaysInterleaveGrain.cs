using Orleans.Concurrency;

namespace Reentrancy;

public static class AlwaysInterleaveSample
{
    public static async Task Run(IGrainFactory factory)
    {
        var grain = factory.GetGrain<IAlwaysInterleaveGrain>(Guid.NewGuid());

        await Task.WhenAll(
            grain.Fast("1"),
            grain.Fast("2"));

        await Task.WhenAll(
            grain.Slow("1 (slow)"),
            grain.Slow("2 (slow)"));

        await Task.WhenAll(
            grain.Slow("1 (slow)"),
            grain.Fast("2"));
    }
}

public interface IAlwaysInterleaveGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task Fast(string str);

    Task Slow(string str);
}

public class AlwaysInterleaveGrain : Grain, IAlwaysInterleaveGrain
{
    public Task Fast(string str) => DoWork(str);

    public Task Slow(string str) => DoWork(str);

    private async Task DoWork(string str)
    {
        Console.WriteLine($"{str} - First");
        await Task.Delay(1000);
        Console.WriteLine($"{str} - Second");
        await Task.Delay(1000);
        Console.WriteLine($"{str} - Third");
        await Task.Delay(1000);
    }
}
using Orleans.Concurrency;

namespace Booking;

public interface IUserSimulatorGrain : IGrainWithGuidKey
{
    [OneWay]
    Task Start();
}
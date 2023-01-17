using Orleans.Concurrency;

namespace Booking;

public interface IRoomObserver : IGrainObserver
{
    [OneWay]
    Task OnAvailabilityChanged(TimeSlot timeSlot);
}
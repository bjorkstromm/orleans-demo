using Orleans.Concurrency;

namespace Booking;

public interface IRoomGrain : IGrainWithStringKey
{
    [ReadOnly]
    Task<IReadOnlyCollection<TimeSlot>> GetTimeSlots(DateOnly date);

    Task Subscribe(IRoomObserver observer);

    Task Unsubscribe(IRoomObserver observer);

    Task SetAvailability(TimeSlot timeSlot);
}
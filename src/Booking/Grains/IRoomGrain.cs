using Booking.Models;
using Orleans.Concurrency;

namespace Booking.Grains;

public interface IRoomGrain : IGrainWithStringKey
{
    [ReadOnly]
    Task<IReadOnlyCollection<TimeSlot>> GetTimeSlots(DateOnly date);

    Task SetAvailability(TimeSlot timeSlot, bool isAvailable);
}
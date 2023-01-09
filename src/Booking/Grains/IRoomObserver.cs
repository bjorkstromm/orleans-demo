using Booking.Models;

namespace Booking.Grains;

public interface IRoomObserver : IGrainObserver
{
    Task OnAvailabilityChanged(TimeSlot timeSlot);
}
namespace Booking;

public interface IRoomObserver : IGrainObserver
{
    Task OnAvailabilityChanged(TimeSlot timeSlot);
}
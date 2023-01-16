namespace Booking;

public interface IReservationObserver : IGrainObserver
{
    Task OnReservationExpired();
}
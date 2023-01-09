namespace Booking.Grains;

public interface IReservationObserver : IGrainObserver
{
    Task OnReservationExpired();
}
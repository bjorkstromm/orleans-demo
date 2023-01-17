using Orleans.Concurrency;

namespace Booking;

public interface IReservationObserver : IGrainObserver
{
    [OneWay]
    Task OnReservationExpired();
}
using Booking.Models;

namespace Booking.Grains;

public interface ITimeSlotGrain : IGrainWithStringKey
{
    Task<Reservation> Reserve(IReservationObserver observer);

    Task<bool> CancelReservation(string reservationId);

    Task<bool> Book(string reservationId);
}
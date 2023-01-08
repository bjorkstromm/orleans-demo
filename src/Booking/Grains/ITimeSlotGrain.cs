using Booking.Models;

namespace Booking.Grains;

public interface ITimeSlotGrain : IGrainWithStringKey
{
    Task<Reservation> Reserve();

    Task<bool> CancelReservation(string reservationId);

    Task<bool> Book(string reservationId);
}
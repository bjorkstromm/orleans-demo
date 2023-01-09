namespace Booking.Models;

[GenerateSerializer]
public record Reservation(string TimeSlotId, string? ReservationId, DateTimeOffset? ExpiresOn)
{
    public bool Success => ReservationId is not null;

    public static Reservation Fail => new(string.Empty, null, null);
};
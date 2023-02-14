using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Booking.Grains;

public class TimeSlotGrain : Grain, ITimeSlotGrain, IRemindable
{
    private readonly IPersistentState<State> _state;
    private readonly IGrainFactory _grainFactory;

    private const string ClearReservationReminder = nameof(ClearReservationReminder);

    [GenerateSerializer]
    public record State
    {
        [Id(0)]
        public string? ReservationId { get; init; }

        public bool IsAvailable => ReservationId is null;
    }

    public TimeSlotGrain(
        [PersistentState("timeslot")] IPersistentState<State> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public async Task<Reservation> Reserve()
    {
        // Because TimeSlot information is encoded in the grain id,
        // try to parse it.
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return Reservation.Fail;
        }

        // Can't reserve a slot that's not available.
        if (!_state.State.IsAvailable)
        {
            return Reservation.Fail;
        }

        // Create a reservation id, and set reservation
        // expiration time (use 20 seconds for demo purposes)
        var id = Guid.NewGuid().ToString("N");
        var expiresOn = DateTimeOffset.UtcNow.AddSeconds(20);

        _state.State = new State { ReservationId = id };

        await _state.WriteStateAsync();

        // Inform the parent room about the reservation.
        var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);
        await room.SetAvailability(timeSlot with { Available = false });

        // Register a reminder for clearing the reservation
        // after it's expiration.
        await this.RegisterOrUpdateReminder(ClearReservationReminder,
            expiresOn - DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5));

        return new Reservation(this.GetPrimaryKeyString(), id, expiresOn);
    }

    public async Task<bool> CancelReservation(string reservationId)
    {
        // Because TimeSlot information is encoded in the grain id, try to parse it.
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return false;
        }

        // Can't cancel a reservation that's not ours.
        if (!string.Equals(reservationId, _state.State.ReservationId, StringComparison.Ordinal))
        {
            return false;
        }

        // Clearing state, will clear all information about the reservation.
        await _state.ClearStateAsync();

        // Inform the parent room that reservation was canceled.
        var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);
        await room.SetAvailability(timeSlot with { Available = true });

        // Unregister the reminder. No need to execute because reservation was canceled manually.
        var reminder = await this.GetReminder(ClearReservationReminder);
        await this.UnregisterReminder(reminder);

        return true;
    }

    public async Task<bool> Book(string reservationId)
    {
        // Because TimeSlot information is encoded in the grain id, try to parse it.
        if (!string.Equals(reservationId, _state.State.ReservationId, StringComparison.Ordinal))
        {
            return false;
        }

        // TODO: Make actual booking here :)
        // Would probably call to an external service,
        // or maybe a different grain.

        // Unregister the reminder for clearing reservation.
        // Should not execute because time-slot was booked.
        var reminder = await this.GetReminder(ClearReservationReminder);
        await this.UnregisterReminder(reminder);

        return true;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // Because TimeSlot information is encoded in the grain id, try to parse it.
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return;
        }

        if (string.Equals(ClearReservationReminder, reminderName, StringComparison.Ordinal))
        {
            await _state.ClearStateAsync();

            var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);
            await room.SetAvailability(timeSlot with { Available = true });

            var reminder = await this.GetReminder(reminderName);
            await this.UnregisterReminder(reminder);
        }
    }
}
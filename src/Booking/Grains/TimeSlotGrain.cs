using Booking.Models;
using Orleans.Runtime;

namespace Booking.Grains;

public class TimeSlotGrain : Grain, ITimeSlotGrain, IRemindable
{
    private readonly IPersistentState<State> _state;
    private readonly IGrainFactory _grainFactory;
    private IReservationObserver? _observer;

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

    public async Task<Reservation> Reserve(IReservationObserver observer)
    {
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return Reservation.Fail;
        }

        if (!_state.State.IsAvailable)
        {
            return Reservation.Fail;
        }

        var id = Guid.NewGuid().ToString("N");
        var expiresOn = DateTimeOffset.UtcNow.AddSeconds(20);

        _state.State = new State { ReservationId = id };

        await _state.WriteStateAsync();

        var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);

        await room.SetAvailability(timeSlot, isAvailable: false);

        await this.RegisterOrUpdateReminder(ClearReservationReminder,
            expiresOn - DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5));

        _observer = observer;

        return new Reservation(this.GetPrimaryKeyString(), id, expiresOn);
    }

    public async Task<bool> CancelReservation(string reservationId)
    {
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return false;
        }

        if (!string.Equals(reservationId, _state.State.ReservationId, StringComparison.Ordinal))
        {
            return false;
        }

        await _state.ClearStateAsync();

        var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);
        await room.SetAvailability(timeSlot, isAvailable: false);

        _observer = null;

        return true;
    }

    public async Task<bool> Book(string reservationId)
    {
        if (!string.Equals(reservationId, _state.State.ReservationId, StringComparison.Ordinal))
        {
            return false;
        }

        var reminder = await this.GetReminder(ClearReservationReminder);

        await this.UnregisterReminder(reminder);

        _observer = null;

        return true;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (!TimeSlot.TryParse(this.GetPrimaryKeyString(), out var timeSlot))
        {
            return;
        }

        if (string.Equals(ClearReservationReminder, reminderName, StringComparison.Ordinal))
        {
            await _state.ClearStateAsync();

            var room = _grainFactory.GetGrain<IRoomGrain>(timeSlot.RoomId);
            await room.SetAvailability(timeSlot, isAvailable: true);

            var reminder = await this.GetReminder(reminderName);
            await this.UnregisterReminder(reminder);

            if (_observer is not null)
            {
                await _observer.OnReservationExpired();
                _observer = null;
            }
        }
    }
}
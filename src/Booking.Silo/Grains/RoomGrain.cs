using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Utilities;

namespace Booking.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<State> _state;
    private readonly ObserverManager<IRoomObserver> _observers;

    public RoomGrain(
        [PersistentState("room")] IPersistentState<State> state,
        ILogger<RoomGrain> logger)
    {
        _state = state;
        _observers = new ObserverManager<IRoomObserver>(TimeSpan.FromMinutes(5), logger);
    }

    public Task<IReadOnlyCollection<TimeSlot>> GetTimeSlots(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return Task.FromResult<IReadOnlyCollection<TimeSlot>>(Array.Empty<TimeSlot>());
        }

        var days = to.DayNumber - from.DayNumber + 1;

        // Just create some time-slots for the sake of the example.
        // In a real application, this could come from an external service.
        var slots =
            Enumerable.Range(0, days)
                .SelectMany(day =>
                    Enumerable.Range(0, 8)
                        .Select(offset => new TimeSlot(
                            RoomId: this.GetPrimaryKeyString(),
                            Date: from.AddDays(day),
                            Start: new TimeOnly(8 + offset, 0),
                            End: new TimeOnly(9 + offset, 0),
                            Available: true)))
                // Set the availability based on the state.
                .Select(x => x with { Available = !_state.State.NotAvailable.Contains(x.Id) })
                .ToArray();

        return Task.FromResult<IReadOnlyCollection<TimeSlot>>(slots);
    }

    public async Task SetAvailability(TimeSlot timeSlot)
    {
        // Verify that the time-slot is valid.
        if (timeSlot.RoomId != this.GetPrimaryKeyString())
        {
            return;
        }

        if (timeSlot.Available)
        {
            // If time-slot is available, remove it from the list of unavailable time-slots.
            _state.State.NotAvailable.Remove(timeSlot.Id);
        }
        else
        {
            // else, add it to the list of unavailable time-slots.
            _state.State.NotAvailable.Add(timeSlot.Id);
        }

        // Save state
        await _state.WriteStateAsync();

        // Notify observers
        await _observers.Notify(x =>
            x.OnAvailabilityChanged(timeSlot));
    }

    public Task Subscribe(IRoomObserver observer)
    {
        _observers.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IRoomObserver observer)
    {
        _observers.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    [GenerateSerializer]
    public record State
    {
        [Id(0)]
        public HashSet<string> NotAvailable { get; init; } = new();
    }
}
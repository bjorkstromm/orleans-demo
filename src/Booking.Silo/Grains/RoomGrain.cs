using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Utilities;

namespace Booking.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<State> _state;
    private readonly ObserverManager<IRoomObserver> _observers;

    [GenerateSerializer]
    public record State
    {
        [Id(0)]
        public HashSet<string> NotAvailable { get; init; } = new();
    }

    public RoomGrain(
        [PersistentState("room")] IPersistentState<State> state,
        ILogger<RoomGrain> logger)
    {
        _state = state;
        _observers = new ObserverManager<IRoomObserver>(TimeSpan.FromMinutes(5), logger);
    }

    public Task<IReadOnlyCollection<TimeSlot>> GetTimeSlots(DateOnly date)
    {
        var slots =
            Enumerable.Range(0, 8)
                .Select(offset => new TimeSlot(
                    RoomId: this.GetPrimaryKeyString(),
                    Date: date,
                    Start: new TimeOnly(8 + offset, 0),
                    End: new TimeOnly(9 + offset, 0),
                    Available: true))
                .Select(x => x with { Available = !_state.State.NotAvailable.Contains(x.Id) })
                .ToArray();

        return Task.FromResult<IReadOnlyCollection<TimeSlot>>(slots);
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

    public async Task SetAvailability(TimeSlot timeSlot)
    {
        if (timeSlot.RoomId != this.GetPrimaryKeyString())
        {
            return;
        }

        if (timeSlot.Available)
        {
            _state.State.NotAvailable.Remove(timeSlot.Id);
        }
        else
        {
            _state.State.NotAvailable.Add(timeSlot.Id);
        }

        await _state.WriteStateAsync();
        await _observers.Notify(x =>
            x.OnAvailabilityChanged(timeSlot));
    }
}
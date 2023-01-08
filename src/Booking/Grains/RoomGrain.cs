using Booking.Models;
using Orleans.Runtime;

namespace Booking.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<State> _state;

    [GenerateSerializer]
    public record State
    {
        [Id(0)]
        public HashSet<string> NotAvailable { get; init; } = new();
    }

    public RoomGrain(
        [PersistentState("room")] IPersistentState<State> state)
    {
        _state = state;
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

    public async Task SetAvailability(TimeSlot timeSlot, bool isAvailable)
    {
        if (timeSlot.RoomId != this.GetPrimaryKeyString())
        {
            return;
        }

        if (isAvailable)
        {
            _state.State.NotAvailable.Remove(timeSlot.Id);
        }
        else
        {
            _state.State.NotAvailable.Add(timeSlot.Id);
        }

        await _state.WriteStateAsync();
    }
}
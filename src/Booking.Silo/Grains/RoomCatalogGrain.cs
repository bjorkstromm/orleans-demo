using Orleans.Runtime;

namespace Booking.Grains;

public class RoomCatalogGrain : Grain, IRoomCatalogGrain
{
    private readonly IPersistentState<State> _state;

    [GenerateSerializer]
    public record State
    {
        [Id(0)]
        public Dictionary<string, string> Rooms { get; init; } = new();
    }

    public RoomCatalogGrain(
        [PersistentState("rooms")] IPersistentState<State> state)
    {
        _state = state;
    }

    public Task<IReadOnlyCollection<Room>> GetRooms()
    {
        var rooms = _state.State.Rooms
            .Select(x => new Room(x.Key, x.Value))
            .OrderBy(x => x.Name)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Room>>(rooms);
    }

    public async Task AddRoom(string name)
    {
        var id = Guid.NewGuid().ToString("N");
        _state.State.Rooms.Add(id, name);

        await _state.WriteStateAsync();
    }
}
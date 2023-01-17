using Orleans.Runtime;

namespace Booking.Grains;

public class RoomCatalogGrain : Grain, IRoomCatalogGrain
{
    private IReadOnlyCollection<Room> _rooms = Array.Empty<Room>();
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

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        UpdateCache();

        return base.OnActivateAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<Room>> GetRooms() => Task.FromResult(_rooms);

    public async Task AddRoom(string name)
    {
        var id = Guid.NewGuid().ToString("N");
        _state.State.Rooms.Add(id, name);

        await _state.WriteStateAsync();

        UpdateCache();
    }

    public async Task DeleteRoom(string id)
    {
        if (_state.State.Rooms.Remove(id))
        {
            await _state.WriteStateAsync();

            UpdateCache();
        }
    }

    private void UpdateCache() =>
        _rooms = _state.State.Rooms
            .Select(x => new Room(x.Key, x.Value))
            .OrderBy(x => x.Name)
            .ToArray();
}
using Orleans.Concurrency;

namespace Booking;

public interface IRoomCatalogGrain : IGrainWithIntegerKey
{
    [ReadOnly]
    Task<IReadOnlyCollection<Room>> GetRooms();

    Task AddRoom(string name);

    Task DeleteRoom(string id);
}
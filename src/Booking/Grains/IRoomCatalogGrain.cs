using Booking.Models;
using Orleans.Concurrency;

namespace Booking.Grains;

public interface IRoomCatalogGrain : IGrainWithIntegerKey
{
    [ReadOnly]
    Task<IReadOnlyCollection<Room>> GetRooms();

    Task AddRoom(string name);
}
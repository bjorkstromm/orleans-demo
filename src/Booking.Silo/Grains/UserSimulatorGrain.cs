using Orleans.Runtime;

namespace Booking.Grains;

public class UserSimulatorGrain : Grain, IUserSimulatorGrain, IRemindable
{
    private readonly IPersistentState<State> _state;
    private readonly IGrainFactory _grainFactory;
    private IDisposable? _timer;
    private IReadOnlyCollection<Room> _rooms;

    [GenerateSerializer]
    public class State
    {
        public int Count { get; set; }
    }

    public UserSimulatorGrain(
        [PersistentState("timeslot")] IPersistentState<State> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var catalog = _grainFactory.GetGrain<IRoomCatalogGrain>(0);
        _rooms = await catalog.GetRooms();

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // Write state to storage
        await _state.WriteStateAsync();

        // if we are getting deactivated, due to silo going down,
        // let's re-register the timer so that we'll hopefully wake up in another silo after 5 seconds
        if (_state.State.Count < 100)
        {
            await this.RegisterOrUpdateReminder("reminder",
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMinutes(1));
        }
    }

    public async Task Start()
    {
        await this.RegisterOrUpdateReminder("reminder",
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        _timer = RegisterTimer(OnTick, string.Empty, TimeSpan.FromSeconds(Random.Shared.Next(10)), TimeSpan.FromSeconds(5));
    }

    private async Task OnTick(object? state)
    {
        // End after 1000 iterations
        if (_state.State.Count >= 100)
        {
            _timer?.Dispose();

            return;
        }

        var timeSlotGrain = _grainFactory.GetGrain<ITimeSlotGrain>(RandomTimeSlot().Id);
        await timeSlotGrain.Reserve();

        _state.State.Count++;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // Wake up timer if it's not running
        if (_state.State.Count < 100 && _timer is null)
        {
            _timer = RegisterTimer(OnTick, string.Empty, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        }
        else if (_state.State.Count >= 100) // End after 100 iterations
        {
            var reminder = await this.GetReminder(reminderName);
            await this.UnregisterReminder(reminder);
        }
    }

    private TimeSlot RandomTimeSlot()
    {
        var roomId = _rooms.ToArray()[Random.Shared.Next(_rooms.Count)].Id;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(Random.Shared.Next(365)));
        var start = new TimeOnly(8 + Random.Shared.Next(8), 0);
        var end = start.AddHours(1);

        return new TimeSlot(roomId, date, start, end, false);
    }
}
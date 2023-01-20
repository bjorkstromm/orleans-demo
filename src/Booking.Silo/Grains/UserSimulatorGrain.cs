using Orleans.Runtime;

namespace Booking.Grains;

public class UserSimulatorGrain : Grain, IUserSimulatorGrain, IRemindable, IReservationObserver
{
    private readonly IPersistentState<State> _state;
    private readonly IGrainFactory _grainFactory;
    private IDisposable? _timer;

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

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
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

        _timer = RegisterTimer(OnTick, null, TimeSpan.FromSeconds(Random.Shared.Next(10)), TimeSpan.FromSeconds(5));
    }

    private async Task OnTick(object? state)
    {
        // End after 1000 iterations
        if (_state.State.Count >= 100)
        {
            _timer?.Dispose();

            return;
        }

        var catalog = _grainFactory.GetGrain<IRoomCatalogGrain>(0);
        var rooms = await catalog.GetRooms();

        var roomId = rooms.ToArray()[Random.Shared.Next(rooms.Count)].Id;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(Random.Shared.Next(1, 365)));

        var roomGrain = _grainFactory.GetGrain<IRoomGrain>(roomId);
        var timeSlots = await roomGrain.GetTimeSlots(date);

        var availableTimeSlots = timeSlots.Where(x => x.Available).ToArray();

        if (availableTimeSlots.Length == 0)
        {
            return;
        }

        var timeSlot = availableTimeSlots[Random.Shared.Next(availableTimeSlots.Length)];

        var timeSlotGrain = _grainFactory.GetGrain<ITimeSlotGrain>(timeSlot.Id);

        await timeSlotGrain.Reserve(GrainReference.AsReference<IReservationObserver>());

        _state.State.Count++;
        await _state.WriteStateAsync();
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // Wake up timer if it's not running
        if (_state.State.Count < 100 && _timer is null)
        {
            _timer = RegisterTimer(OnTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        }
        else if (_state.State.Count >= 100) // End after 100 iterations
        {
            var reminder = await this.GetReminder(reminderName);
            await this.UnregisterReminder(reminder);
        }
    }

    public Task OnReservationExpired() => Task.CompletedTask;
}
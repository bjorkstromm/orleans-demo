using System.Diagnostics;

namespace Booking.Admin;

public class ActivityScope : IDisposable
{
    private readonly Activity? _previous;
    private readonly ActivitySource _source;
    private readonly Activity? _current;

    public static ActivityScope Create(string name) => new(name);

    private ActivityScope(string name)
    {
        _previous = Activity.Current;
        Activity.Current = null;

        _source = new ActivitySource("booking-admin");
        _current = _source.StartActivity(name);
    }

    public void Dispose()
    {
        _current?.Dispose();
        _source?.Dispose();

        Activity.Current = _previous;
    }
}
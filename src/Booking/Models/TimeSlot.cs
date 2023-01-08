using System.Globalization;

namespace Booking.Models;

[GenerateSerializer]
public record TimeSlot(
    string RoomId,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    bool Available)
{
    public string Id =>
        Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                FormattableString.Invariant(
                    $"{RoomId}\0{Date:yyyy-MM-dd}\0{Start:HH:mm}\0{End:HH:mm}")));

    public static bool TryParse(string id, out TimeSlot timeSlot)
    {
        timeSlot = null!;

        var buffer = new Span<byte>(new byte[id.Length]);
        if (!Convert.TryFromBase64String(id, buffer, out var length))
        {
            return false;
        }

        var decoded = System.Text.Encoding.UTF8.GetString(buffer[..length]);
        var tokens = decoded.Split('\0');

        if (tokens.Length != 4)
        {
            return false;
        }

        var roomId = tokens[0];

        if (!DateOnly.TryParseExact(tokens[1], "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(tokens[2], "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(tokens[3], "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return false;
        }

        timeSlot = new TimeSlot
        (
            RoomId: roomId,
            Date: date,
            Start: start,
            End: end,
            Available: false
        );

        return true;
    }
}
using System.Globalization;

namespace Luma.Client.Encoders;

public class Iso8601Encoder : ITimestampEncoder
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";

    public long Decode(string encoded)
    {
        if (DateTimeOffset.TryParseExact(encoded, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.UtcTicks;
        }
        
        // Fallback to general parsing if exact format fails (e.g. slight variations)
        return DateTimeOffset.Parse(encoded, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).UtcTicks;
    }

    public string Encode(long ticks)
    {
        var dto = new DateTimeOffset(ticks, TimeSpan.Zero);
        return dto.ToString(Format, CultureInfo.InvariantCulture);
    }
}

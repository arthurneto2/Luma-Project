using System.Globalization;

namespace Luma.Client.Encoders;

public class TicksPureEncoder : ITimestampEncoder
{
    public long Decode(string encoded)
    {
        return long.Parse(encoded, CultureInfo.InvariantCulture);
    }

    public string Encode(long ticks)
    {
        return ticks.ToString(CultureInfo.InvariantCulture);
    }
}

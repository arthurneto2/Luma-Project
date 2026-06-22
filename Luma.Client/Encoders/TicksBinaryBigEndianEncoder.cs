using System.Buffers.Binary;

namespace Luma.Client.Encoders;

public class TicksBinaryBigEndianEncoder : ITimestampEncoder
{
    public long Decode(string encoded)
    {
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length != 8)
        {
            throw new ArgumentException($"Invalid byte length for TicksBinaryBigEndian. Expected 8, got {bytes.Length}", nameof(encoded));
        }
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    public string Encode(long ticks)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, ticks);
        return Convert.ToBase64String(bytes);
    }
}

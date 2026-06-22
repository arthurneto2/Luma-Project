using System.Buffers.Binary;

namespace Luma.Client.Encoders;

public class TicksBinaryEncoder : ITimestampEncoder
{
    public long Decode(string encoded)
    {
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length != 8)
        {
            throw new ArgumentException($"Invalid byte length for TicksBinary. Expected 8, got {bytes.Length}", nameof(encoded));
        }
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    public string Encode(long ticks)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, ticks);
        return Convert.ToBase64String(bytes);
    }
}

namespace Luma.Client.Encoders;

public interface ITimestampEncoder
{
    long Decode(string encoded);
    string Encode(long ticks);
}

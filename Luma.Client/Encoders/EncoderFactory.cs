namespace Luma.Client.Encoders;

public static class EncoderFactory
{
    private static readonly ITimestampEncoder Iso8601 = new Iso8601Encoder();
    private static readonly ITimestampEncoder TicksPure = new TicksPureEncoder();
    private static readonly ITimestampEncoder TicksBinary = new TicksBinaryEncoder();
    private static readonly ITimestampEncoder TicksBinaryBigEndian = new TicksBinaryBigEndianEncoder();

    public static ITimestampEncoder GetEncoder(string encoding)
    {
        return encoding switch
        {
            "Iso8601" => Iso8601,
            "Ticks" => TicksPure,
            "TicksBinary" => TicksBinary,
            "TicksBinaryBigEndian" => TicksBinaryBigEndian,
            _ => throw new ArgumentException($"Unknown encoding type: {encoding}", nameof(encoding))
        };
    }
}

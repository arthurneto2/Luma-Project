using Luma.Client.Encoders;
using Luma.Client.Models;

namespace Luma.Client.Services;

public class TimeSyncService
{
    public string ProbeId { get; }
    public string ProbeName { get; }
    public ITimestampEncoder Encoder { get; }
    public double TimeDilationFactor { get; }
    public long AccumulatedOffset { get; private set; }
    public long BaseEarthTicks { get; private set; }
    public long LastRoundTrip { get; private set; }
    public bool IsSynchronized { get; private set; }

    // 5 milliseconds in Ticks (1 ms = 10,000 ticks)
    private const long ThresholdTicks = 5 * 10000;

    public TimeSyncService(Probe probe)
    {
        ProbeId = probe.Id;
        ProbeName = probe.Name;
        Encoder = EncoderFactory.GetEncoder(probe.Encoding);
        TimeDilationFactor = probe.TimeDilationFactor ?? 1.0;
        AccumulatedOffset = 0;
        IsSynchronized = false;
    }

    public void UpdateSync(long t0, long t1, long t2, long t3)
    {
        long adjustedT0;
        long adjustedT3;

        if (BaseEarthTicks == 0)
        {
            adjustedT0 = t0;
            adjustedT3 = t3;

            long newOffset = ((t1 - adjustedT0) + (t2 - adjustedT3)) / 2;
            long roundTrip = (t3 - t0) - (t2 - t1);

            AccumulatedOffset = newOffset;
            LastRoundTrip = roundTrip;
            BaseEarthTicks = t3;

            if (Math.Abs(newOffset) < ThresholdTicks)
            {
                IsSynchronized = true;
            }
        }
        else
        {
            // Calculate elapsed Earth time since the first sync call's end (BaseEarthTicks)
            // and divide by TimeDilationFactor to find elapsed Probe time.
            long elapsedEarth0 = t0 - BaseEarthTicks;
            long elapsedProbe0 = (long)Math.Round(elapsedEarth0 / TimeDilationFactor);
            adjustedT0 = BaseEarthTicks + AccumulatedOffset + elapsedProbe0;

            long elapsedEarth3 = t3 - BaseEarthTicks;
            long elapsedProbe3 = (long)Math.Round(elapsedEarth3 / TimeDilationFactor);
            adjustedT3 = BaseEarthTicks + AccumulatedOffset + elapsedProbe3;

            long newOffset = ((t1 - adjustedT0) + (t2 - adjustedT3)) / 2;
            long roundTrip = (t3 - t0) - (t2 - t1);

            AccumulatedOffset += newOffset;
            LastRoundTrip = roundTrip;

            if (Math.Abs(newOffset) < ThresholdTicks)
            {
                IsSynchronized = true;

                // Capture BaseEarthTicks as t3 of the last successful sync return
                // Adjust AccumulatedOffset to be relative to the new BaseEarthTicks (t3)
                long probeTimeAtT3 = BaseEarthTicks + AccumulatedOffset + elapsedProbe3;
                AccumulatedOffset = probeTimeAtT3 - t3;
                BaseEarthTicks = t3;
            }
        }
    }

    public long GetCurrentProbeTicks()
    {
        if (!IsSynchronized)
        {
            // If not synchronized, we default to standard offset calculation
            return DateTimeOffset.UtcNow.Ticks + AccumulatedOffset;
        }

        long tNow = DateTimeOffset.UtcNow.Ticks;
        long elapsedEarth = tNow - BaseEarthTicks;

        // Apply time dilation: elapsedProbe = elapsedEarth / TimeDilationFactor
        long elapsedProbe = (long)Math.Round(elapsedEarth / TimeDilationFactor);

        return BaseEarthTicks + AccumulatedOffset + elapsedProbe;
    }

    public void Reset()
    {
        AccumulatedOffset = 0;
        BaseEarthTicks = 0;
        LastRoundTrip = 0;
        IsSynchronized = false;
    }
}
